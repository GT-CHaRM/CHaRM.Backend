module CHaRM.Util.Option

[<RequireQualifiedAccess>]
module TryGet =
    let (|Some|None|) input =
        match input with
        | true, value -> Some value
        | _ -> None

type MaybeBuilder() =
    member __.Bind (x, f) = Option.bind f x
    member __.Return x = Some x
    member __.ReturnFrom x = x
    member __.Zero () = None

let maybe = MaybeBuilder()
