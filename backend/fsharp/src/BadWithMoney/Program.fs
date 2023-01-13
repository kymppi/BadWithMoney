open Falco
open Falco.Routing
open Falco.HostBuilder
open Marten
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

let configureMarten (configuration: IConfiguration) (storeOptions: StoreOptions) =
  storeOptions.Connection(configuration.GetConnectionString("Postgresql"))

let configureServices (configuration: IConfiguration) (serviceCollection: IServiceCollection) =
  serviceCollection.AddMarten(configureMarten configuration) |> ignore
  serviceCollection

[<EntryPoint>]
let main args =
  let configuration = configuration args { add_env }

  webHost args {
    add_service (configureServices configuration)
    endpoints [ post "/budgets" HttpHandlers.createBudgetHandler ]
  }

  0
