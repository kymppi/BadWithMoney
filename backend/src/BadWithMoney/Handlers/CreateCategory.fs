module Handlers.CreateCategory

open Domain
open Falco
open Validus
open Marten

type CreateBudgetCategoryRequest = {
  CategoryName: string
  AmountAllocated: decimal
}

type ValidatedCreateBudgetCategoryRequest = {
  CategoryName: CategoryName
  AmountAllocated: PositiveDecimal
}

let validateRequest (request: CreateBudgetCategoryRequest) = validate {
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
      do! Response.conflict $"The category `{request.CategoryName}` already exists" ctx
    | Ok budget ->
      do! saveBudget budget ctx.RequestAborted
      do! Response.ofJson budget ctx
  }

let handler: HttpHandler =
  Request.requiresAuthentication (
    Services.inject<IQuerySession, IDocumentSession> (fun querySession documentSession ->
      let findBudgetById = Provider.findBudgetById querySession
      let saveBudget = Provider.saveBudget documentSession

      Request.mapValidateJson
        validateRequest
        (fun validatedRequest ->
          Request.mapRoute
            (fun reader -> BudgetId.create (reader.GetGuid "budgetId"))
            (fun budgetId ->
              Authorization.budgetAuthorizer
                (fun ctx -> findBudgetById budgetId ctx.RequestAborted)
                (createBudgetCategory saveBudget validatedRequest)))
        Response.validationProblemDetails)
  )
