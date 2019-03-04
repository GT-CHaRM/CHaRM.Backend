[<AutoOpen>]
module CHaRM.Backend.Services.Item

open System
open System.Threading.Tasks

open CHaRM.Backend.Model

type IItemService =
    abstract member All: unit -> ItemType [] Task
    abstract member Create: {|Name: string|} -> ItemType Task
    abstract member Get: {|Id: Guid|} -> ItemType Task


(* Mock implementations *)

let newItem name =
    {
        Id = Guid.NewGuid ()
        Name = name
    }

let mutable items = [|
    yield newItem "Paint"
    yield newItem "Tires"
    yield newItem "Hazardous Chemicals"
    yield newItem "Electronics"
    yield newItem "Styrofoam"
    yield newItem "Metal"
    yield newItem "Mattresses"
    yield newItem "Textiles"
    yield newItem "Glass"
|]

type ItemService () =
    interface IItemService with
        member __.All () = Task.FromResult items
        member __.Create args =
            let item = {
                Id = Guid.NewGuid ()
                Name = args.Name
            }
            items <- [|yield item; yield! items|]
            Task.FromResult item
        member __.Get args =
            items
            |> Array.find (fun {Id = id} -> args.Id = id)
            |> Task.FromResult
