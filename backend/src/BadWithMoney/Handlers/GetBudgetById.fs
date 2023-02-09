module Handlers.GetBudgetById

open Domain
open Falco
open Marten

type PopularCategory = CategoryName * decimal
type PopularCategories = Map<CategoryName, decimal>

type RecentTransactionType =
  | Income = 1
  | Expense = 2

type RecentTransaction = {
  Type: RecentTransactionType
  Amount: decimal
}

type QueryParams = {
  NumberOfCategories: int option
  NumberOfTransactions: int option
}

type BudgetResponse = {
  Id: BudgetId
  Name: string
  PopularCategories: PopularCategories
  RecentTransactions: RecentTransaction list
}

let private createPopularCategory (category: Category, spent: decimal) : PopularCategory = category.Name, spent

let private createRecentTransaction (transaction: Transaction) =
  let type', details =
    match transaction with
    | Income details -> RecentTransactionType.Income, details
    | Expense details -> RecentTransactionType.Expense, details

  {
    Type = type'
    Amount = decimal details.Amount
  }

let private readQueryParams (reader: QueryCollectionReader) = {
  NumberOfCategories = reader.TryGetInt("categories")
  NumberOfTransactions = reader.TryGetInt("transactions")
}

let private createBudgetResponse (queryParams: QueryParams) (budget: Budget) =
  let minOrDefault value =
    value |> Option.map (min 20) |> Option.defaultValue 10

  let numberOfCategories = queryParams.NumberOfCategories |> minOrDefault
  let numberOfTransactions = queryParams.NumberOfTransactions |> minOrDefault

  let popularCategories =
    Budget.popularCategories numberOfCategories budget
    |> List.map createPopularCategory
    |> Map.ofList

  let recentTransactions =
    Budget.recentTransactions numberOfTransactions budget
    |> List.map createRecentTransaction

  {
    Id = budget.Id
    Name = string budget.Name
    PopularCategories = popularCategories
    RecentTransactions = recentTransactions
  }

let handler: HttpHandler =
  Request.requiresAuthentication (
    Services.inject<IQuerySession> (fun querySession ->
      let findBudgetById = Provider.findBudgetById querySession

      Request.mapRoute
        (fun reader -> BudgetId.create (reader.GetGuid "budgetId"))
        (fun budgetId ->
          Request.mapQuery readQueryParams (fun queryParams ->
            Authorization.budgetAuthorizer
              (fun ctx -> findBudgetById budgetId ctx.RequestAborted)
              (fun budget -> createBudgetResponse queryParams budget |> Response.ofJson))))
  )
