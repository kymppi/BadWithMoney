[<AutoOpen>]
module Http

open System.Security.Claims
open Domain
open Falco
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Http
open Validus

type ProblemDetails = {
  Type: string
  Title: string
  Status: int
  Detail: string
  Instance: string
  Errors: Map<string, string list>
}

[<RequireQualifiedAccess>]
module ProblemDetails =
  let createValidationProblemDetails instance errors = {
    Type = "https://httpstatuses.com/400"
    Title = "One or more validation errors occured."
    Status = 400
    Detail = "Please refer to the errors property for additional details."
    Instance = instance
    Errors = errors
  }

[<RequireQualifiedAccess>]
module Request =
  let getUserId (ctx: HttpContext) =
    ctx.User.FindFirst(ClaimTypes.NameIdentifier)
    |> Option.ofNull
    |> Option.map (fun claim -> UserId.create claim.Value)

  let mapValidateJson
    (validator: 'a -> ValidationResult<'b>)
    (onSuccess: 'b -> HttpHandler)
    (onValidationErrors: ValidationErrors -> HttpHandler)
    =
    let handleOk (record: 'a) : HttpHandler =
      match validator record with
      | Ok result -> onSuccess result
      | Error validationErrors -> onValidationErrors validationErrors

    Request.mapJson handleOk

  let requiresAuthentication successHandler =
    Request.ifAuthenticated
      successHandler
      (Response.withStatusCode StatusCodes.Status401Unauthorized >> Response.ofEmpty)

[<RequireQualifiedAccess>]
module Response =
  let validationProblemDetails (errors: ValidationErrors) : HttpHandler =
    let response instance errors =
      errors
      |> ValidationErrors.toMap
      |> ProblemDetails.createValidationProblemDetails instance
      |> fun response -> (Response.withStatusCode 400 >> Response.ofJson response)

    fun ctx ->
      let path = string ctx.Request.Path
      response path errors ctx

  let statusCode code : HttpHandler =
    Response.withStatusCode code >> Response.ofEmpty

  let unauthorized: HttpHandler = statusCode StatusCodes.Status401Unauthorized
  let forbidden: HttpHandler = statusCode StatusCodes.Status403Forbidden

  let notFound message : HttpHandler =
    Response.withStatusCode StatusCodes.Status404NotFound
    >> Response.ofPlainText message

  let conflict message : HttpHandler =
    Response.withStatusCode StatusCodes.Status409Conflict
    >> Response.ofPlainText message
