module Handlers.GetAllBudgets

open Falco
open Domain
open Marten

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

let private getBudgets (getBudgetsForUser: Provider.GetBudgetsForUser) : HttpHandler =
  fun ctx -> task {
    match Request.getUserId ctx with
    | None -> do! Response.unauthorized ctx
    | Some userId ->
      let! budgets = getBudgetsForUser userId ctx.RequestAborted

      let response = budgets |> List.map createBudgetDetails |> Response.ofJson
      do! response ctx
  }

let handler: HttpHandler =
  Request.requiresAuthentication (
    Services.inject<IQuerySession> (fun querySession ->
      let getBudgetsForUser = Provider.getBudgetsForUser querySession
      getBudgets getBudgetsForUser)
  )
