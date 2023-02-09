[<RequireQualifiedAccess>]
module Authorization

open System.Threading.Tasks
open Domain
open Falco
open Microsoft.AspNetCore.Http

// TODO: Is this any good?
let inline budgetAuthorizer
  (getBudgetAsync: HttpContext -> Task<Budget option>)
  (onAuthorized: Budget -> HttpHandler)
  : HttpHandler =
  fun ctx -> task {
    let! budget = getBudgetAsync ctx

    let respondWith =
      match budget with
      | None -> Response.notFound "That budget was not found!"
      | Some budget ->
        match Request.getUserId ctx with
        | Some userId when budget.UserId = userId -> onAuthorized budget
        | Some _ -> Response.forbidden
        | None -> Response.unauthorized

    do! respondWith ctx
  }
