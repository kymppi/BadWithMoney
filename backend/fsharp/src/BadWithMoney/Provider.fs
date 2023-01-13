module Provider

open System
open System.Threading
open System.Threading.Tasks
open Marten
open Domain

type FindBudgetById = BudgetId -> CancellationToken -> Task<Budget option>
type SaveBudget = Budget -> CancellationToken -> Task

let findBudgetById (querySession: IQuerySession) : FindBudgetById =
  fun budgetId cancellationToken ->
    querySession
    |> Session.query<Budget>
    |> Queryable.filter <@ fun budget -> budget.Id = budgetId @>
    |> Queryable.tryHeadTask cancellationToken

let saveBudget (documentSession: IDocumentSession) : SaveBudget =
  fun budget cancellationToken ->
    documentSession |> Session.storeSingle budget
    documentSession |> Session.saveChangesTask cancellationToken
