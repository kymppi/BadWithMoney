module Handlers.Authentication

open System.Security.Claims
open Falco
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.Extensions.Configuration

let signInHandler (configuration: IConfiguration) : HttpHandler =
  Request.mapQuery
    (fun reader -> reader.TryGetString("redirectUrl"))
    (fun redirectUrl httpContext ->
      let redirectUrl = redirectUrl |> Option.defaultValue "/"
      let clientDomain = configuration["CLIENT_DOMAIN"]
      let properties = AuthenticationProperties(RedirectUri = clientDomain + redirectUrl)
      httpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties))

let logout: HttpHandler =
  fun ctx -> ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)

let claims: HttpHandler =
  Request.requiresAuthentication (fun ctx ->
    let claim (key: string) =
      ctx.User.FindFirst(key)
      |> Option.ofNull
      |> Option.map (fun claim -> claim.Value)
      |> Option.defaultValue ""

    Response.ofJson
      {|
        id = claim ClaimTypes.NameIdentifier
        name = claim ClaimTypes.Name
        email = claim ClaimTypes.Email
      |}
      ctx)
