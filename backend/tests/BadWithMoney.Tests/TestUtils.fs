[<AutoOpen>]
module TestUtils

open Expecto

[<RequireQualifiedAccess>]
module Expect =
  let isOkWith (f: 'a -> bool) (message: string) (result: Result<'a, _>) =
    match result with
    | Ok result when f result -> ()
    | _ -> failtest message

  let isErrorWith (f: 'a -> bool) (message: string) (result: Result<_, 'a>) =
    match result with
    | Error result when f result -> ()
    | _ -> failtest message
