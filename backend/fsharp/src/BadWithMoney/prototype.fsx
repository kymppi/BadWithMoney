open System

type Goal =
    { Name: string
      Cost: decimal
      AmountSaved: decimal }

type Expense =
    { Name: string
      Cost: decimal
      CreatedAt: DateTime }

type Category =
    { Name: string
      MaximumAllocation: decimal
      Expenses: Expense list }

type Budget =
    { Allocation: decimal
      Categories: Category list
      Goals: Goal list }

let maxAmountYouCanAllocate budget =
    budget.Categories
    |> List.sumBy (fun category -> category.MaximumAllocation)
    |> fun allocatedInCategories -> budget.Allocation - allocatedInCategories

let isBudgetOverAllocated budget =
    let maximum = maxAmountYouCanAllocate budget
    maximum < 0m

let category =
    { Name = "Snacks"
      MaximumAllocation = 100.00m
      Expenses = [] }

let budget =
    { Allocation = 1000.00m
      Categories = [ category ]
      Goals = [] }

maxAmountYouCanAllocate budget