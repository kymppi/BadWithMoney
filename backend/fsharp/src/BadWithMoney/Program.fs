open Falco
open Falco.Routing
open Falco.HostBuilder
open Marten
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

let configureMarten (configuration: IConfiguration) (storeOptions: StoreOptions) =
  storeOptions.Connection(configuration.GetConnectionString("Postgresql"))

let configureServices (configuration: IConfiguration) (serviceCollection: IServiceCollection) =
  serviceCollection.AddMarten(configureMarten configuration) |> ignore

  serviceCollection
    .AddAuthentication(fun options ->
      options.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
      options.DefaultChallengeScheme <- GoogleDefaults.AuthenticationScheme
      options.DefaultAuthenticateScheme <- GoogleDefaults.AuthenticationScheme)
    .AddCookie(fun options -> options.LoginPath <- "/google-signin")
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
      get "/google-signin" HttpHandlers.GoogleSignIn.signInHandler
      get "/claims" HttpHandlers.GoogleSignIn.claims
      post "/budgets" HttpHandlers.createBudgetHandler
      post "/budgets/{budgetId}/category" HttpHandlers.createBudgetCategory
      post "/budgets/{budgetId}/category/expense" HttpHandlers.createExpense
    ]
  }

  0
