module Handlers.CreateTransaction

open System
open Domain
open Falco
open Validus
open Marten

type CreateTransactionRequest = {
  CategoryName: string
  Amount: decimal
  TransactionType: string
}

type ValidatedTransactionType =
  | Income
  | Expense

type ValidatedCreateTransactionRequest = {
  CategoryName: CategoryName
  Amount: PositiveDecimal
  TransactionType: ValidatedTransactionType
}

let createTransaction (occuredAt: DateTime) (request: ValidatedCreateTransactionRequest) =
  match request.TransactionType with
  | ValidatedTransactionType.Income ->
    Transaction.Income
      {
        Amount = request.Amount
        OccuredAt = occuredAt
      }
  | ValidatedTransactionType.Expense ->
    Transaction.Expense
      {
        Amount = request.Amount
        OccuredAt = occuredAt
      }

let private transactionTypeValidator (field: string) (input: string) =
  let rule (value: string) =
    match value with
    | "income"
    | "expense" -> true
    | _ -> false

  let toValidatedTransactionType (transactionType: string) =
    match transactionType with
    | "income" -> ValidatedTransactionType.Income
    | "expense" -> ValidatedTransactionType.Expense
    | _ -> failwith "Unexpected transaction type." // <- this should never happen.

  let message = sprintf "%s must be `income` or `expense`"

  input.ToLowerInvariant()
  |> Validator.create message rule field
  |> Result.map toValidatedTransactionType

let private validateRequest (createTransactionRequest: CreateTransactionRequest) = validate {
  let! categoryName = CategoryName.create createTransactionRequest.CategoryName
  let! amount = PositiveDecimal.create createTransactionRequest.Amount
  let! transactionType = transactionTypeValidator "Transaction type" createTransactionRequest.TransactionType

  return {
    CategoryName = categoryName
    Amount = amount
    TransactionType = transactionType
  }
}

let private createTransactionHandler
  (saveBudget: Provider.SaveBudget)
  (request: ValidatedCreateTransactionRequest)
  (budget: Budget)
  : HttpHandler =
  fun ctx -> task {
    let now = DateTime.UtcNow
    let transaction = createTransaction now request

    let createTransactionResult =
      budget |> Budget.transact transaction request.CategoryName

    match createTransactionResult with
    | Error Budget.TransactionError.CategoryDoesNotExist ->
      do! Response.notFound $"The category `{request.CategoryName}` was not found" ctx
    | Ok budget ->
      do! saveBudget budget ctx.RequestAborted
      do! Response.ofJson budget ctx
  }

let handler: HttpHandler =
  Request.requiresAuthentication (
    Services.inject<IQuerySession, IDocumentSession> (fun querySession documentSession ->
      let findBudgetById = Provider.findBudgetById querySession
      let saveBudget = Provider.saveBudget documentSession

      let handler request budgetId =
        Authorization.budgetAuthorizer
          (fun ctx -> findBudgetById budgetId ctx.RequestAborted)
          (createTransactionHandler saveBudget request)

      Request.mapValidateJson
        validateRequest
        (fun request ->
          Request.mapRoute
            (fun reader -> BudgetId.create (reader.GetGuid "budgetId"))
            (fun budgetId -> handler request budgetId))
        Response.validationProblemDetails)
  )
