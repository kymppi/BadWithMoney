[<AutoOpen>]
module Common

open System
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module List =
  let replace (oldElm: 'a) (newElem: 'a) (list: 'a list) =
    let mapper x = if x = oldElm then newElem else x
    List.map mapper list

[<RequireQualifiedAccess>]
module DateTime =
  let toEpoch (dateTime: DateTime) =
    let difference = dateTime - DateTime.UnixEpoch
    int difference.TotalSeconds
