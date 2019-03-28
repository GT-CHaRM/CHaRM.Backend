[<AutoOpen>]
module CHaRM.Backend.Services.Item

open System
open System.Threading.Tasks
open FSharp.Utils.Tasks
open Microsoft.EntityFrameworkCore

open CHaRM.Backend.Error
open CHaRM.Backend.Database
open CHaRM.Backend.Model
open CHaRM.Backend.Util

let all (db: ApplicationDbContext) = task { return! db.Items.ToArrayAsync () }
let create (db: ApplicationDbContext) name description =
    task {
        let changeTracking = db.Items.Add {
            Id = Guid.NewGuid ()
            Name = name
            Description = description
        }
        return changeTracking.Entity
    }
let get (db: ApplicationDbContext) (id: Guid) =
    task {
        match! db.Items.FindAsync id with
        | Default -> return Error [ItemNotFound id]
        | value -> return Ok value
    }
let edit (db: ApplicationDbContext) id name description =
    task {
        let! item = % get db id
        name |> Option.iter (fun name -> item.Name <- name)
        description |> Option.iter (fun description -> item.Description <- description)
        let change = db.Items.Update item
        return Ok change.Entity
    }

type IItemService =
    abstract member All: unit -> ItemType [] Task
    abstract member Create: name: string -> description: string -> ItemType Task
    abstract member Get: id: Guid -> Result<ItemType, ErrorCode list> Task
    abstract member Edit: id: Guid -> name: string option -> description: string option -> Result<ItemType, ErrorCode list> Task

type ItemService (db: ApplicationDbContext) =
    let all () = all db
    let create name description = create db name description
    let get id = get db id
    let edit id name description = edit db id name description

    interface IItemService with
        member __.All () = task { return! all () }
        member __.Create name description = db.changes { return! create name description }
        member __.Get id = task { return! get id }
        member __.Edit id name description = db.changes { return! edit id name description }
