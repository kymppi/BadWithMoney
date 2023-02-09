open System.Text.Json.Serialization
open Domain
open Falco
open Falco.Routing
open Falco.HostBuilder
open Marten
open Marten.Services
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Weasel.Core

let configureMarten (configuration: IConfiguration) (storeOptions: StoreOptions) =
  storeOptions.Connection(configuration.GetConnectionString("Postgresql"))

  let serializer =
    SystemTextJsonSerializer(EnumStorage = EnumStorage.AsString, Casing = Casing.CamelCase)

  // https://www.jannikbuschke.de/blog/fsharp-marten/
  serializer.Customize(fun options ->
    options.Converters.Add(
      JsonFSharpConverter(
        JsonUnionEncoding.AdjacentTag
        ||| JsonUnionEncoding.NamedFields
        ||| JsonUnionEncoding.UnwrapRecordCases
        ||| JsonUnionEncoding.UnwrapOption
        ||| JsonUnionEncoding.UnwrapSingleCaseUnions
        ||| JsonUnionEncoding.AllowUnorderedTag,
        allowNullFields = false
      )
    ))

  storeOptions.Serializer(serializer)
  storeOptions.RegisterDocumentType<Budget>()

let configureServices (configuration: IConfiguration) (serviceCollection: IServiceCollection) =
  serviceCollection.AddMarten(configureMarten configuration) |> ignore

  serviceCollection
    .AddAuthentication(fun options ->
      options.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
      options.DefaultChallengeScheme <- GoogleDefaults.AuthenticationScheme
      options.DefaultAuthenticateScheme <- GoogleDefaults.AuthenticationScheme)
    .AddCookie(fun options ->
      options.LoginPath <- "/api/google-signin"
      options.Cookie.Name <- "auth")
    .AddGoogle(fun options ->
      options.ClientId <- configuration["GOOGLE_CLIENT_ID"]
      options.ClientSecret <- configuration["GOOGLE_SECRET"]
      options.CallbackPath <- "/google-callback")
  |> ignore

  serviceCollection

[<EntryPoint>]
let main args =
  let configuration = configuration args { add_env }

  webHost args {
    add_service (configureServices configuration)
    use_authentication
    use_authorization

    endpoints [
      get "/api/google-signin" (Handlers.Authentication.signInHandler configuration)
      post "/api/logout" Handlers.Authentication.logout
      get "/api/me" Handlers.Authentication.claims
      post "/api/budgets" Handlers.CreateBudget.handler
      get "/api/budgets" Handlers.GetAllBudgets.handler
      get "/api/budgets/{budgetId}" Handlers.GetBudgetById.handler
      post "/api/budgets/{budgetId}/category" Handlers.CreateCategory.handler
      post "/api/budgets/{budgetId}/category/transaction" Handlers.CreateTransaction.handler
    ]
  }

  0
