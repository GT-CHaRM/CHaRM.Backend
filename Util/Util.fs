namespace CHaRM.Util

[<AutoOpen>]
module Unit =
    let (!) = ignore

module List =
    let filterMap fFilter fMap =
        List.filter fFilter
        >> List.map fMap
    let filterCollect fFilter fMap =
        List.filter fFilter
        >> List.collect fMap

module Seq =
    let filterMap fFilter fMap =
        Seq.filter fFilter
        >> Seq.map fMap
    let filterCollect fFilter fMap =
        Seq.filter fFilter
        >> Seq.collect fMap

module Integer =
    let digits = float >> log10 >> ceil >> int

module String =
    let inline private get (s: string) = s
    let contains needle haystack = (get haystack).Contains (get needle)
    let isEmpty input = get input |> String.length |> (=) 0
