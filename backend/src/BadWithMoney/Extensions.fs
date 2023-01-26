[<AutoOpen>]
module Extensions

[<RequireQualifiedAccess>]
module List =
  let replace (oldElm: 'a) (newElem: 'a) (list: 'a list) =
    let mapper x = if x = oldElm then newElem else x
    List.map mapper list
