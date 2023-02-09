module Handlers.CreateBudget

open System
open Domain
open Falco
open Marten
open Validus

type CreateBudgetRequest = {
  Name: string
  AllocableAmount: decimal
}

type ValidatedCreateBudgetRequest = {
  Name: BudgetName
  AllocableAmount: PositiveDecimal
}

type BudgetDetails = {
  Id: BudgetId
  Name: string
  CreatedAt: int
  UpdatedAt: int
}

let createBudgetDetails (budget: Budget) = {
  Id = budget.Id
  Name = string budget.Name
  CreatedAt = DateTime.toEpoch budget.CreatedAt
  UpdatedAt = DateTime.toEpoch budget.UpdatedAt
}

let private validateRequest (createBudgetRequest: CreateBudgetRequest) = validate {
  let! budgetName = BudgetName.create createBudgetRequest.Name
  let! monthlyIncome = PositiveDecimal.create createBudgetRequest.AllocableAmount

  return {
    Name = budgetName
    AllocableAmount = monthlyIncome
  }
}

let private createBudget (saveBudget: Provider.SaveBudget) (request: ValidatedCreateBudgetRequest) : HttpHandler =
  fun ctx -> task {
    match Request.getUserId ctx with
    | None -> do! Response.unauthorized ctx
    | Some userId ->
      let now = DateTime.UtcNow
      let budgetId = BudgetId.createNew ()

      let budget = Budget.create now budgetId userId request.Name request.AllocableAmount
      let budgetDetailsResponse = budget |> createBudgetDetails |> Response.ofJson

      do! saveBudget budget ctx.RequestAborted
      do! budgetDetailsResponse ctx
  }

let handler: HttpHandler =
  Request.requiresAuthentication (
    Services.inject<IDocumentSession> (fun documentSession ->
      let saveBudget = Provider.saveBudget documentSession
      let createBudget request = createBudget saveBudget request
      Request.mapValidateJson validateRequest createBudget Response.validationProblemDetails)
  )
