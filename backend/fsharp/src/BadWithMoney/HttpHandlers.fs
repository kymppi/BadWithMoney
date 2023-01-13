module HttpHandlers

open System
open Falco
open System.Threading.Tasks
open FSharp.UMX
open Microsoft.AspNetCore.Http
open Validus
open Domain
open Marten

let unauthorizedHandler: HttpHandler =
  Response.withStatusCode StatusCodes.Status401Unauthorized >> Response.ofEmpty

let notFoundHandler: HttpHandler =
  Response.withStatusCode StatusCodes.Status404NotFound >> Response.ofEmpty

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
        let budget = Budget.create budgetId request.Name request.MonthlyIncome
        do! saveBudget budget ctx.RequestAborted
        do! Response.ofJson budget ctx
      }

    let handleValidationErrors = Response.validationProblemDetails "/budget"
    Request.mapValidateJson validateRequest createBudget handleValidationErrors

module CreateBudgetCategory =
  type CreateBudgetCategoryRequest = {
    BudgetId: Guid
    CategoryName: string
    AmountAllocated: decimal
  }

  type ValidatedCreateBudgetCategoryRequest = {
    BudgetId: BudgetId
    CategoryName: CategoryName
    AmountAllocated: PositiveDecimal
  }

  let validateRequest (request: CreateBudgetCategoryRequest) : ValidationResult<ValidatedCreateBudgetCategoryRequest> = validate {
    let budgetId = BudgetId.create request.BudgetId
    let! categoryName = CategoryName.create request.CategoryName
    let! amountAllocated = PositiveDecimal.create request.AmountAllocated

    return {
      BudgetId = budgetId
      CategoryName = categoryName
      AmountAllocated = amountAllocated
    }
  }

  let handler (findBudgetById: Provider.FindBudgetById) (saveBudget: Provider.SaveBudget) : HttpHandler =
    let createBudgetCategory (request: ValidatedCreateBudgetCategoryRequest) : HttpHandler =
      fun ctx -> task {
        let! budget = findBudgetById request.BudgetId ctx.RequestAborted

        match budget with
        | None -> do! notFoundHandler ctx
        | Some budget ->
          match Budget.createCategory request.CategoryName request.AmountAllocated budget with
          | Error error -> do! Response.ofJson {| error = error |} ctx
          | Ok budget ->
            do! saveBudget budget ctx.RequestAborted
            do! Response.ofJson budget ctx
      }

    let handleValidationErrors = Response.validationProblemDetails "/budget/category"
    Request.mapValidateJson validateRequest createBudgetCategory handleValidationErrors

module CreateExpense =
  type CreateExpenseRequest = {
    BudgetId: Guid
    CategoryName: string
    Details: string
    Amount: decimal
  }

  type ValidatedCreateExpenseRequest = {
    BudgetId: BudgetId
    CategoryName: CategoryName
    Details: NonEmptyString
    Amount: PositiveDecimal
  }

  let validateRequest (createExpenseRequest: CreateExpenseRequest) : ValidationResult<ValidatedCreateExpenseRequest> = validate {
    let budgetId = BudgetId.create createExpenseRequest.BudgetId
    let! categoryName = CategoryName.create createExpenseRequest.CategoryName
    let! details = NonEmptyString.create createExpenseRequest.Details
    let! amount = PositiveDecimal.create createExpenseRequest.Amount

    return {
      BudgetId = budgetId
      CategoryName = categoryName
      Details = details
      Amount = amount
    }
  }

  let handler (findBudgetById: Provider.FindBudgetById) (saveBudget: Provider.SaveBudget) : HttpHandler =
    let createExpense (request: ValidatedCreateExpenseRequest) : HttpHandler =
      fun ctx -> task {
        let! budget = findBudgetById request.BudgetId ctx.RequestAborted

        match budget with
        | None -> do! notFoundHandler ctx
        | Some budget ->
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

    let handleValidationErrors =
      Response.validationProblemDetails "/budget/category/expense"

    Request.mapValidateJson validateRequest createExpense handleValidationErrors

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
