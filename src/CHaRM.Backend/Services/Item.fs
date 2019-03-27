[<AutoOpen>]
module CHaRM.Backend.Services.Item

open System
open System.Threading.Tasks
open FSharp.Utils.Tasks
open Microsoft.EntityFrameworkCore

open CHaRM.Backend.Database
open CHaRM.Backend.Model

type IItemService =
    abstract member All: unit -> ItemType [] Task
    abstract member Create: name: string -> ItemType Task
    abstract member Get: id: Guid -> ItemType Task

type ItemService (dbContext: ApplicationDbContext) =
    interface IItemService with
        member __.All () = task { return! dbContext.Items.ToArrayAsync () }
        member __.Create name =
            task {
                let item = {
                    Id = Guid.NewGuid ()
                    Name = name
                }
                let! changeTracking = dbContext.Items.AddAsync item
                return changeTracking.Entity
            }
        member __.Get id = task { return! dbContext.Items.FindAsync id }
