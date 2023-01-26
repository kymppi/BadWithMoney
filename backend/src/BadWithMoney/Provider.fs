module Provider

open System.Threading
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Marten
open Domain

type FindBudgetById = BudgetId -> CancellationToken -> Task<Budget option>
type GetBudgetsForUser = UserId -> CancellationToken -> Task<Budget list>
type SaveBudget = Budget -> CancellationToken -> Task

let findBudgetById (querySession: IQuerySession) : FindBudgetById =
  fun budgetId cancellationToken ->
    querySession
    |> Session.query<Budget>
    |> Queryable.filter <@ fun budget -> budget.Id = budgetId @>
    |> Queryable.tryHeadTask cancellationToken

let getBudgetsForUser (querySession: IQuerySession) =
  fun userId ->
    querySession
    |> Session.query<Budget>
    |> Queryable.filter <@ fun budget -> budget.UserId = userId @>
    |> Queryable.toListAsync
    |> Async.map Seq.toList

let saveBudget (documentSession: IDocumentSession) : SaveBudget =
  fun budget cancellationToken ->
    documentSession |> Session.storeSingle budget
    documentSession |> Session.saveChangesTask cancellationToken
