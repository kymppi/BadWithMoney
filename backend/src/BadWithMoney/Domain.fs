module Domain

open System
open FSharp.UMX
open Validus
open Validus.Operators

[<Measure>]
type userId

[<Measure>]
type budgetId

type UserId = string<userId>
type BudgetId = Guid<budgetId>

module UserId =
  let create (value: string) : string<userId> = %value

type BudgetName =
  private
  | BudgetName of string

  override this.ToString() =
    let (BudgetName name) = this
    name

type CategoryName =
  private
  | CategoryName of string

  override this.ToString() =
    let (CategoryName name) = this
    name

type PositiveDecimal =
  private
  | PositiveDecimal of decimal

  static member op_Explicit(PositiveDecimal decimal) : decimal = decimal

type NonEmptyString =
  private
  | NonEmptyString of string

  override this.ToString() =
    let (NonEmptyString value) = this
    value

type Goal = {
  Name: NonEmptyString
  Cost: PositiveDecimal
  AmountSaved: PositiveDecimal
}

type TransactionDetails = {
  Amount: PositiveDecimal
  OccuredAt: DateTime
}

type Transaction =
  | Income of TransactionDetails
  | Expense of TransactionDetails

type Category = {
  Name: CategoryName
  Allocation: PositiveDecimal
  Transactions: Transaction list
}

type Budget = {
  Id: BudgetId
  UserId: UserId
  Name: BudgetName
  MaximumAllocable: PositiveDecimal
  Categories: Category list
  Goals: Goal list
  CreatedAt: DateTime
  UpdatedAt: DateTime
}

[<RequireQualifiedAccess>]
module BudgetId =
  let create (id: Guid) : BudgetId = %id

[<RequireQualifiedAccess>]
module BudgetName =
  let create name =
    let validator = Check.String.notEmpty <+> Check.String.lessThanLen 150

    validate {
      let! name = validator "Budget name" name
      return BudgetName name
    }

[<RequireQualifiedAccess>]
module CategoryName =
  let create name =
    let validator = Check.String.notEmpty <+> Check.String.lessThanLen 75

    validate {
      let! name = validator "Category" name
      return CategoryName name
    }

[<RequireQualifiedAccess>]
module PositiveDecimal =
  let create value = validate {
    let! name = Check.Decimal.greaterThanOrEqualTo 0m "amount" value
    return PositiveDecimal name
  }

[<RequireQualifiedAccess>]
module NonEmptyString =
  // TODO: Pass in field name? 'Value' is quite generic.
  let create value =
    let validator = Check.String.notEmpty

    validate {
      let! value = validator "Value" value
      return NonEmptyString value
    }

[<RequireQualifiedAccess>]
module Budget =
  let create now budgetId userId name maximumAllocable = {
    Id = budgetId
    UserId = userId
    Name = name
    MaximumAllocable = maximumAllocable
    Categories = []
    Goals = []
    CreatedAt = now
    UpdatedAt = now
  }

  let getBudgetCategory name budget =
    budget.Categories |> List.tryFind (fun c -> c.Name = name)

  type TransactionError = CategoryDoesNotExist

  let transact transaction categoryName budget =
    match getBudgetCategory categoryName budget with
    | None -> Error TransactionError.CategoryDoesNotExist
    | Some category ->
      let transactions = transaction :: category.Transactions

      let newCategory =
        { category with
            Transactions = transactions
        }

      let categories = budget.Categories |> List.replace category newCategory
      Ok { budget with Categories = categories }

  type CreateCategoryError = CategoryAlreadyExists

  let createCategory categoryName allocation budget =
    match getBudgetCategory categoryName budget with
    | Some _ -> Error CreateCategoryError.CategoryAlreadyExists
    | None ->
      let category = {
        Name = categoryName
        Allocation = allocation
        Transactions = []
      }

      let categories = category :: budget.Categories
      Ok { budget with Categories = categories }

  let sumExpensesForCategory category =
    let calculateExpense transaction =
      match transaction with
      | Income _ -> 0m
      | Expense details -> decimal details.Amount

    category.Transactions |> List.sumBy calculateExpense

  let sumExpenses budget =
    budget.Categories |> List.sumBy sumExpensesForCategory

  let popularCategories n budget =
    budget.Categories
    |> List.map (fun category -> category, sumExpensesForCategory category)
    |> List.sortByDescending snd
    |> List.truncate n

  let recentTransactions n budget =
    let occuredAt transaction =
      match transaction with
      | Income details -> details.OccuredAt
      | Expense details -> details.OccuredAt

    budget.Categories
    |> List.collect (fun category -> category.Transactions)
    |> List.sortByDescending occuredAt
    |> List.truncate n
