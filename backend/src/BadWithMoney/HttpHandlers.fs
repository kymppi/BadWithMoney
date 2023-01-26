module HttpHandlers

open System
open System.Security.Claims
open Falco
open System.Threading.Tasks
open FSharp.UMX
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Validus
open Domain
open Marten

let unauthorizedHandler: HttpHandler =
  Response.withStatusCode StatusCodes.Status401Unauthorized >> Response.ofEmpty

let forbiddenHandler: HttpHandler =
  Response.withStatusCode StatusCodes.Status403Forbidden >> Response.ofEmpty

let notFoundHandler: HttpHandler =
  Response.withStatusCode StatusCodes.Status404NotFound >> Response.ofEmpty

// TODO: Is this any good?
let inline budgetAuthorizer
  (f: HttpContext -> Task<Budget option>)
  (onAuthorized: Budget -> HttpHandler)
  : HttpHandler =
  fun ctx -> task {
    let! budget = f ctx

    match budget with
    | None -> do! notFoundHandler ctx
    | Some budget ->
      match Request.getUserId ctx with
      | Some userId when budget.UserId = userId -> do! onAuthorized budget ctx
      | _ -> do! forbiddenHandler ctx
  }

module GoogleSignIn =
  let signInHandler (configuration: IConfiguration) : HttpHandler =
    Request.mapQuery
      (fun reader -> reader.GetString("redirectUrl"))
      (fun redirectUrl httpContext ->
        let clientDomain = configuration["CLIENT_DOMAIN"]
        let properties = AuthenticationProperties(RedirectUri = clientDomain + redirectUrl)
        httpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties))

  // NOTE(sheridanchris): this is only for development, should be removed in the future.
  let claims: HttpHandler =
    fun ctx -> task {
      let claims = ctx.User.Claims |> Seq.map (fun claim -> claim.Type, claim.Value)
      do! Response.ofJson claims ctx
    }

module GetBudgets =
  let handler (getBudgetsForUser: Provider.GetBudgetsForUser) : HttpHandler =
    fun ctx -> task {
      match Request.getUserId ctx with
      | None -> do! unauthorizedHandler ctx
      | Some userId ->
        let! budgets = getBudgetsForUser userId ctx.RequestAborted
        do! Response.ofJson budgets ctx
    }

module CreateBudget =
  type CreateBudgetRequest = { Name: string; MonthlyIncome: decimal }

  type ValidatedCreateBudgetRequest = {
    Name: BudgetName
    MonthlyIncome: PositiveDecimal
  }

  let validateRequest (createBudgetRequest: CreateBudgetRequest) = validate {
    let! budgetName = BudgetName.create createBudgetRequest.Name
    let! monthlyIncome = PositiveDecimal.create createBudgetRequest.MonthlyIncome

    return {
      Name = budgetName
      MonthlyIncome = monthlyIncome
    }
  }

  let handler (saveBudget: Provider.SaveBudget) : HttpHandler =
    let createBudget request : HttpHandler =
      fun ctx -> task {
        let budgetId = % Guid.NewGuid()

        match Request.getUserId ctx with
        | None -> do! unauthorizedHandler ctx
        | Some userId ->
          let budget = Budget.create budgetId userId request.Name request.MonthlyIncome
          do! saveBudget budget ctx.RequestAborted
          do! Response.ofJson budget ctx
      }

    let handleValidationErrors = Response.validationProblemDetails "/budget"
    Request.mapValidateJson validateRequest createBudget handleValidationErrors

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
      | Error error -> do! Response.ofJson {| error = error |} ctx
      | Ok budget ->
        do! saveBudget budget ctx.RequestAborted
        do! Response.ofJson budget ctx
    }

  let handler (findBudgetById: Provider.FindBudgetById) (saveBudget: Provider.SaveBudget) : HttpHandler =
    let handleCreateCategoryRequest (request: ValidatedCreateBudgetCategoryRequest) : HttpHandler =
      fun ctx -> task {
        let routeParameters = Request.getRoute ctx
        let budgetId = BudgetId.create (routeParameters.GetGuid "budgetId")

        do!
          budgetAuthorizer
            (fun ctx -> findBudgetById budgetId ctx.RequestAborted)
            (createBudgetCategory saveBudget request)
            ctx
      }

    let handleValidationErrors = Response.validationProblemDetails "/budget/category"
    Request.mapValidateJson validateRequest handleCreateCategoryRequest handleValidationErrors

module CreateExpense =
  type CreateExpenseRequest = {
    CategoryName: string
    Details: string
    Amount: decimal
  }

  type ValidatedCreateExpenseRequest = {
    CategoryName: CategoryName
    Details: NonEmptyString
    Amount: PositiveDecimal
  }

  let validateRequest (createExpenseRequest: CreateExpenseRequest) : ValidationResult<ValidatedCreateExpenseRequest> = validate {
    let! categoryName = CategoryName.create createExpenseRequest.CategoryName
    let! details = NonEmptyString.create createExpenseRequest.Details
    let! amount = PositiveDecimal.create createExpenseRequest.Amount

    return {
      CategoryName = categoryName
      Details = details
      Amount = amount
    }
  }

  let private createExpense
    (saveBudget: Provider.SaveBudget)
    (request: ValidatedCreateExpenseRequest)
    (budget: Budget)
    : HttpHandler =
    fun ctx -> task {
      let now = DateOnly.FromDateTime(DateTime.UtcNow)

      let createExpenseResult =
        Budget.createExpense
          request.CategoryName
          {
            Details = request.Details
            Amount = request.Amount
            Date = now
          }
          budget

      match createExpenseResult with
      | Error error -> do! Response.ofJson {| error = error |} ctx
      | Ok budget ->
        do! saveBudget budget ctx.RequestAborted
        do! Response.ofJson budget ctx
    }

  let handler (findBudgetById: Provider.FindBudgetById) (saveBudget: Provider.SaveBudget) : HttpHandler =
    let handleCreateExpenseRequest (request: ValidatedCreateExpenseRequest) : HttpHandler =
      fun ctx -> task {
        let routeParameters = Request.getRoute ctx
        let budgetId = BudgetId.create (routeParameters.GetGuid "budgetId")

        do!
          budgetAuthorizer
            (fun ctx -> findBudgetById budgetId ctx.RequestAborted)
            (createExpense saveBudget request)
            ctx
      }

    let handleValidationErrors =
      Response.validationProblemDetails "/budget/category/expense"

    Request.mapValidateJson validateRequest handleCreateExpenseRequest handleValidationErrors

let requireAuthentication successHandler =
  Request.ifAuthenticated successHandler unauthorizedHandler

let createBudgetHandler: HttpHandler =
  requireAuthentication (Services.inject<IDocumentSession> (Provider.saveBudget >> CreateBudget.handler))

let createBudgetCategory: HttpHandler =
  requireAuthentication (
    Services.inject<IQuerySession, IDocumentSession> (fun querySession documentSession ->
      let saveBudget = Provider.saveBudget documentSession
      let findBudgetById = Provider.findBudgetById querySession
      CreateBudgetCategory.handler findBudgetById saveBudget)
  )

let createExpense: HttpHandler =
  requireAuthentication (
    Services.inject<IQuerySession, IDocumentSession> (fun querySession documentSession ->
      let saveBudget = Provider.saveBudget documentSession
      let findBudgetById = Provider.findBudgetById querySession
      CreateExpense.handler findBudgetById saveBudget)
  )
