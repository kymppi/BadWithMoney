open Falco
open Falco.Routing
open Falco.HostBuilder

[<EntryPoint>]
let main args =
    webHost args { endpoints [ get "/" (Response.ofPlainText "Hello, World!") ] }
    0
