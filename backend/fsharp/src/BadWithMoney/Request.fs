﻿[<AutoOpen>]
module RequestUtils

open Validus
open Falco

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

[<RequireQualifiedAccess>]
module Response =
  let validationProblemDetails (instance: string) (errors: ValidationErrors) : HttpHandler =
    errors
    |> ValidationErrors.toMap
    |> ProblemDetails.createValidationProblemDetails instance
    |> fun response -> Response.withStatusCode 400 >> Response.ofJson response
