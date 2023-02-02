module HttpHandlers

open System
open System.Security.Claims
open Falco
open System.Threading.Tasks
open FSharp.UMX
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Validus
open Domain
open Marten

let toEpoch (dateTime: DateTime) =
  let difference = dateTime - DateTime.UnixEpoch
  int difference.TotalSeconds

let min (number1: int) (number2: int) = Math.Min(number1, number2)

let statusCodeResponse code : HttpHandler =
  Response.withStatusCode code >> Response.ofEmpty

let unauthorized: HttpHandler = statusCodeResponse StatusCodes.Status401Unauthorized
let forbidden: HttpHandler = statusCodeResponse StatusCodes.Status403Forbidden

let notFound message : HttpHandler =
  Response.withStatusCode StatusCodes.Status404NotFound
  >> Response.ofPlainText message

let conflict message : HttpHandler =
  Response.withStatusCode StatusCodes.Status409Conflict
  >> Response.ofPlainText message

let requireAuthentication successHandler =
  Request.ifAuthenticated successHandler unauthorized

// TODO: Is this any good?
let inline budgetAuthorizer
  (getBudgetAsync: HttpContext -> Task<Budget option>)
  (onAuthorized: Budget -> HttpHandler)
  : HttpHandler =
  fun ctx -> task {
    let! budget = getBudgetAsync ctx

    match budget with
    | None -> do! notFound "That budget was not found!" ctx
    | Some budget ->
      match Request.getUserId ctx with
      | Some userId when budget.UserId = userId -> do! onAuthorized budget ctx
      | Some _ -> do! forbidden ctx
      | None -> do! unauthorized ctx
  }

module GoogleSignIn =
  let signInHandler (configuration: IConfiguration) : HttpHandler =
    Request.mapQuery
      (fun reader -> reader.GetString("redirectUrl"))
      (fun redirectUrl httpContext ->
        let clientDomain = configuration["CLIENT_DOMAIN"]
        let properties = AuthenticationProperties(RedirectUri = clientDomain + redirectUrl)
        httpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties))

  let logout: HttpHandler =
    fun ctx -> ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)

  let claims: HttpHandler =
    requireAuthentication (fun ctx ->
      let claim (key: string) =
        ctx.User.FindFirst(key)
        |> Option.ofNull
        |> Option.map (fun claim -> claim.Value)
        |> Option.defaultValue ""

      Response.ofJson
        {|
          id = claim ClaimTypes.NameIdentifier
          name = claim ClaimTypes.Name
          email = claim ClaimTypes.Email
        |}
        ctx)

module GetBudgets =
  let handler: HttpHandler =
    requireAuthentication (
      Services.inject<IQuerySession> (fun querySession ctx -> task {
        let getBudgetsForUser = Provider.getBudgetsForUser querySession

        match Request.getUserId ctx with
        | None -> do! unauthorized ctx
        | Some userId ->
          let! budgets = getBudgetsForUser userId ctx.RequestAborted

          let createDto budget = {|
            id = budget.Id
            name = budget.Name
            created = toEpoch budget.CreatedAt
            updated = toEpoch budget.UpdatedAt
          |}

          let budgets = budgets |> List.map createDto
          do! Response.ofJson budgets ctx
      })
    )

module GetBudgetById =
  let private popularCategoryDto (category: Category, spent: decimal) = category.Name, spent

  let private recentTransactionDto (transaction: Transaction) =
    match transaction with
    | Income details -> {|
        transactionType = "income"
        amount = decimal details.Amount
      |}
    | Expense details -> {|
        transactionType = "expense"
        amount = decimal details.Amount
      |}

  let handler: HttpHandler =
    requireAuthentication (
      Services.inject<IQuerySession> (fun querySession ->
        let findBudgetById = Provider.findBudgetById querySession

        Request.mapRoute
          (fun reader -> BudgetId.create (reader.GetGuid "budgetId"))
          (fun budgetId ->
            Request.mapQuery
              (fun reader -> {|
                numberOfCategories = reader.TryGetInt("categories")
                numberOfTransactions = reader.TryGetInt("transactions")
              |})
              (fun query ->
                let numberOfCategories =
                  query.numberOfCategories |> Option.map (min 20) |> Option.defaultValue 10

                let numberOfTransactions =
                  query.numberOfTransactions |> Option.map (min 20) |> Option.defaultValue 10

                budgetAuthorizer
                  (fun ctx -> findBudgetById budgetId ctx.RequestAborted)
                  (fun budget ->
                    let popularCategories =
                      Budget.popularCategories numberOfCategories budget
                      |> List.map popularCategoryDto
                      |> Map.ofList

                    let recentTransactions =
                      Budget.recentTransactions numberOfTransactions budget
                      |> List.map recentTransactionDto

                    Response.ofJson
                      {|
                        id = budget.Id
                        name = budget.Name
                        popularCategories = popularCategories
                        recentTransactions = recentTransactions
                      |}))))
    )

module CreateBudget =
  type CreateBudgetRequest = {
    Name: string
    AllocableAmount: decimal
  }

  type ValidatedCreateBudgetRequest = {
    Name: BudgetName
    AllocableAmount: PositiveDecimal
  }

  let validateRequest (createBudgetRequest: CreateBudgetRequest) = validate {
    let! budgetName = BudgetName.create createBudgetRequest.Name
    let! monthlyIncome = PositiveDecimal.create createBudgetRequest.AllocableAmount

    return {
      Name = budgetName
      AllocableAmount = monthlyIncome
    }
  }

  let createBudget (saveBudget: Provider.SaveBudget) (request: ValidatedCreateBudgetRequest) : HttpHandler =
    fun ctx -> task {
      let budgetId = % Guid.NewGuid()

      match Request.getUserId ctx with
      | None -> do! unauthorized ctx
      | Some userId ->
        let now = DateTime.UtcNow
        let budget = Budget.create now budgetId userId request.Name request.AllocableAmount
        do! saveBudget budget ctx.RequestAborted
        do! Response.ofJson budget ctx
    }

  let handler: HttpHandler =
    requireAuthentication (
      Services.inject<IDocumentSession> (fun documentSession ->
        let saveBudget = Provider.saveBudget documentSession
        Request.mapValidateJson validateRequest (createBudget saveBudget) Response.validationProblemDetails)
    )

module CreateBudgetCategory =
  type CreateBudgetCategoryRequest = {
    CategoryName: string
    AmountAllocated: decimal
  }

  type ValidatedCreateBudgetCategoryRequest = {
    CategoryName: CategoryName
    AmountAllocated: PositiveDecimal
  }

  let validateRequest (request: CreateBudgetCategoryRequest) : ValidationResult<ValidatedCreateBudgetCategoryRequest> = validate {
    let! categoryName = CategoryName.create request.CategoryName
    let! amountAllocated = PositiveDecimal.create request.AmountAllocated

    return {
      CategoryName = categoryName
      AmountAllocated = amountAllocated
    }
  }

  let private createBudgetCategory
    (saveBudget: Provider.SaveBudget)
    (request: ValidatedCreateBudgetCategoryRequest)
    (budget: Budget)
    : HttpHandler =
    fun ctx -> task {
      let createCategoryResult =
        Budget.createCategory request.CategoryName request.AmountAllocated budget

      match createCategoryResult with
      | Error Budget.CreateCategoryError.CategoryAlreadyExists ->
        do! conflict $"The category `{request.CategoryName}` already exists" ctx
      | Ok budget ->
        do! saveBudget budget ctx.RequestAborted
        do! Response.ofJson budget ctx
    }

  let handler: HttpHandler =
    requireAuthentication (
      Services.inject<IQuerySession, IDocumentSession> (fun querySession documentSession ->
        let findBudgetById = Provider.findBudgetById querySession
        let saveBudget = Provider.saveBudget documentSession

        Request.mapValidateJson
          validateRequest
          (fun validatedRequest ->
            Request.mapRoute
              (fun reader -> BudgetId.create (reader.GetGuid "budgetId"))
              (fun budgetId ->
                budgetAuthorizer
                  (fun ctx -> findBudgetById budgetId ctx.RequestAborted)
                  (createBudgetCategory saveBudget validatedRequest)))
          Response.validationProblemDetails)
    )

// TODO: Need to change this to CreateTransaction and account for the type.
module CreateTransaction =
  type TransactionType =
    | Income = 1
    | Expense = 2

  type CreateTransactionRequest = {
    CategoryName: string
    Amount: decimal
    TransactionType: TransactionType
  }

  type ValidatedCreateTransactionRequest = {
    CategoryName: CategoryName
    Amount: PositiveDecimal
    TransactionType: TransactionType
  }

  let validateRequest
    (createTransactionRequest: CreateTransactionRequest)
    : ValidationResult<ValidatedCreateTransactionRequest> =
    validate {
      let! categoryName = CategoryName.create createTransactionRequest.CategoryName
      let! amount = PositiveDecimal.create createTransactionRequest.Amount

      return {
        CategoryName = categoryName
        Amount = amount
        TransactionType = createTransactionRequest.TransactionType
      }
    }

  let private createTransaction
    (saveBudget: Provider.SaveBudget)
    (request: ValidatedCreateTransactionRequest)
    (budget: Budget)
    : HttpHandler =
    fun ctx -> task {
      let now = DateTime.UtcNow

      let transaction =
        match request.TransactionType with
        | TransactionType.Income ->
          Transaction.Income
            {
              Amount = request.Amount
              OccuredAt = now
            }
        | TransactionType.Expense ->
          Transaction.Expense
            {
              Amount = request.Amount
              OccuredAt = now
            }
        | _ -> failwith "Invalid transaction type." // TODO: Should probably create a validator for this.

      let createTransactionResult =
        Budget.transact transaction request.CategoryName budget

      match createTransactionResult with
      | Error Budget.TransactionError.CategoryDoesNotExist ->
        do! notFound $"The category `{request.CategoryName}` was not found" ctx
      | Ok budget ->
        do! saveBudget budget ctx.RequestAborted
        do! Response.ofJson budget ctx
    }

  let handler: HttpHandler =
    requireAuthentication (
      Services.inject<IQuerySession, IDocumentSession> (fun querySession documentSession ->
        let findBudgetById = Provider.findBudgetById querySession
        let saveBudget = Provider.saveBudget documentSession

        Request.mapValidateJson
          validateRequest
          (fun request ->
            Request.mapRoute
              (fun reader -> BudgetId.create (reader.GetGuid "budgetId"))
              (fun budgetId ->
                budgetAuthorizer
                  (fun ctx -> findBudgetById budgetId ctx.RequestAborted)
                  (createTransaction saveBudget request)))
          Response.validationProblemDetails)
    )
