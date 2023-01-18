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

type AllocableAmount =
  | AllocableAmount of decimal

  static member op_Explicit(AllocableAmount decimal) : decimal = decimal

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

type Expense = {
  Details: NonEmptyString
  Amount: PositiveDecimal
  Date: DateOnly
}

type Category = {
  Name: CategoryName
  Allocation: PositiveDecimal
  Expenses: Expense list
}

type TransactionType =
  | Deposit
  | Withdrawl

type Transaction = {
  Reason: NonEmptyString
  Amount: PositiveDecimal
  Type: TransactionType
  Date: DateOnly
}

type Budget = {
  Id: BudgetId
  UserId: UserId
  Name: BudgetName
  MonthlyIncome: PositiveDecimal
  MaximumAllocable: AllocableAmount
  Categories: Category list
  Goals: Goal list
  Transactions: Transaction list
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
module AllocableAmount =
  let isOverAllocated (AllocableAmount amount) = amount < 0m

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
  let create budgetId userId name monthlyIncome =
    let (PositiveDecimal income) = monthlyIncome
    let maximumAllocable = AllocableAmount(income)

    {
      Id = budgetId
      UserId = userId
      Name = name
      MonthlyIncome = monthlyIncome
      MaximumAllocable = maximumAllocable
      Categories = []
      Goals = []
      Transactions = []
    }

  let transact transaction budget =
    let transactions = transaction :: budget.Transactions
    let (AllocableAmount allocableAmount) = budget.MaximumAllocable
    let (PositiveDecimal transactionAmount) = transaction.Amount

    match transaction.Type with
    | Deposit ->
      { budget with
          Transactions = transactions
          MaximumAllocable = AllocableAmount(allocableAmount + transactionAmount)
      }
    | Withdrawl ->
      { budget with
          Transactions = transactions
          MaximumAllocable = AllocableAmount(allocableAmount - transactionAmount)
      }

  let createCategory categoryName allocation budget =
    let existingCategory =
      budget.Categories |> List.tryFind (fun c -> c.Name = categoryName)

    match existingCategory with
    | Some _ -> Error "Category already exists."
    | None ->
      let category = {
        Name = categoryName
        Allocation = allocation
        Expenses = []
      }

      let categories = category :: budget.Categories
      Ok { budget with Categories = categories }

  let createExpense categoryName expense budget =
    let category =
      budget.Categories |> List.tryFind (fun category -> category.Name = categoryName)

    match category with
    | None -> Error "Budget category doesn't exist."
    | Some category ->
      let expenses = expense :: category.Expenses
      let newCategory = { category with Expenses = expenses }
      let categories = budget.Categories |> List.replace category newCategory
      Ok { budget with Categories = categories }

  let sumExpenses budget =
    let sumCategory category =
      category.Expenses |> List.sumBy (fun expense -> decimal expense.Amount)

    budget.Categories |> List.sumBy sumCategory
