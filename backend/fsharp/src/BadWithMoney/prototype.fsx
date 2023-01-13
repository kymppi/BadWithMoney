open System

type Goal =
    { Name: string
      Cost: decimal
      AmountSaved: decimal }

type Expense =
    { Details: string
      Cost: decimal
      CreatedAt: DateTime }

type Category =
    { Name: string
      Allocation: decimal
      Expenses: Expense list }

type TransactionType =
    | Deposit
    | Withdrawl

type Transaction =
    { Amount: decimal
      Type: TransactionType }

type Budget =
    { MaximumAllocable: decimal
      Categories: Category list
      Goals: Goal list
      Transactions: Transaction list }

let transact transaction budget =
    let transactions = transaction :: budget.Transactions

    match transaction.Type with
    | Deposit ->
        { budget with
            MaximumAllocable = budget.MaximumAllocable + transaction.Amount
            Transactions = transactions }
    | Withdrawl ->
        { budget with
            MaximumAllocable = budget.MaximumAllocable - transaction.Amount
            Transactions = transactions }

let maxAmountYouCanAllocate budget =
    budget.Categories
    |> List.sumBy (fun category -> category.Allocation)
    |> fun allocatedInCategories -> budget.MaximumAllocable - allocatedInCategories

let isOverBudget budget =
    let maximum = maxAmountYouCanAllocate budget
    maximum < 0m

let isCategoryOverBudget category =
    let expenses = category.Expenses |> List.sumBy (fun expense -> expense.Cost)
    expenses > category.Allocation

let percentageSpent budget =
    let percentage x y = if y > 0m then Some(y / x) else None

    let sumExpenses expenses =
        expenses |> List.sumBy (fun expense -> expense.Cost)

    let totalSpent =
        budget.Categories |> List.sumBy (fun category -> sumExpenses category.Expenses)

    percentage budget.MaximumAllocable totalSpent
    |> Option.map (fun num -> num * 100m)
    |> Option.defaultValue 0m

let category =
    { Name = "Snacks"
      Allocation = 100.00m
      Expenses =
        [ { Details = "Bought some chips at the store to snack on."
            Cost = 50.00m
            CreatedAt = DateTime.UtcNow } ] }

let budget =
    { MaximumAllocable = 100.00m
      Categories = [ category ]
      Goals = []
      Transactions = [] }

let maxAmount = maxAmountYouCanAllocate budget
let spent = percentageSpent budget
