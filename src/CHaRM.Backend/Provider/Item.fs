[<AutoOpen>]
module CHaRM.Backend.Provider.Item

open System
open System.Threading.Tasks

open CHaRM.Backend.Model

type ItemProvider = {
    All: unit -> ItemType [] Task
    Create: {|Name: string|} -> ItemType Task
    Get: {|Id: Guid|} -> ItemType Task
}

(* Mock implementations *)

let mutable items = [|
    {
        Id = Guid.NewGuid ()
        Name = "myName"
    }
|]
let itemProvider: ItemProvider = {
    All = fun () -> Task.FromResult items
    Create = fun args ->
        let item = {
            Id = Guid.NewGuid ()
            Name = args.Name
        }
        items <- [|yield item; yield! items|]
        Task.FromResult item
    Get = fun args ->
        items
        |> Array.find (fun {Id = id} -> args.Id = id)
        |> Task.FromResult
}
