[<AutoOpen>]
module CHaRM.Backend.Services.Submission

open System
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Utils
open FSharp.Utils.Tasks
open Microsoft.EntityFrameworkCore
open Validation

open CHaRM.Backend.Database
open CHaRM.Backend.Error
open CHaRM.Backend.Model

exception ItemNotFoundException of Id: Guid

type ISubmissionService =
    abstract member All: unit -> Submission [] Task
    abstract member Create: visitor: User -> items: Guid [] -> zipCode: string -> Result<Submission, ErrorCode list> Task
    abstract member Delete: id: Guid -> Result<Submission, ErrorCode list> Task
    abstract member Get: id: Guid -> Result<Submission, ErrorCode list> Task
    abstract member Update: id: Guid -> items: Guid [] -> zipCode: string -> Result<Submission, ErrorCode list> Task

// TODO: Move to utils
module Option =
    let getOrRaiseWith (f: unit -> #exn) option =
        match option with
        | Some value -> value
        | None -> raise (f ())

let makeItemBatch (itemService: IItemService) items =
    task {
        let! allItems = itemService.All ()
        try
            return
                items
                |> Array.groupBy id
                |> Array.map (fun (id, group) ->
                    let count = Array.length group
                    {
                        Id = Guid.NewGuid ()
                        Item =
                            allItems
                            |> Array.tryFind (fun item -> item.Id = id)
                            |> Option.getOrRaiseWith (fun () -> raise (ItemNotFoundException id))
                        Count = count
                    })
                |> Ok
        with :? ItemNotFoundException as exn -> return Error [ItemNotFound exn.Id]
    }

let create (dbContext: ApplicationDbContext) itemService (itemId: Guid) visitor items zipCode =
    task {
        let! items = % makeItemBatch itemService items
        let! result = dbContext.Submissions.AddAsync {
            Id = itemId
            Visitor = visitor
            Submitted = DateTimeOffset.Now
            Items = items
            ZipCode = zipCode
        }
        return Ok result.Entity
    }

type SubmissionService (dbContext, itemService) =
    let create = create dbContext itemService

    interface ISubmissionService with
        member __.All () = task { return! dbContext.Submissions.ToArrayAsync () }

        member __.Create visitor items zipCode = create (Guid.NewGuid ()) visitor items zipCode

        member this.Delete id =
            task {
                let! submission = % (this :> ISubmissionService).Get id
                let result = dbContext.Submissions.Remove submission
                return Ok result.Entity
            }

        member __.Get id =
            task {
                let (|DefaultSubmission|_|) submission =
                    if submission = Unchecked.defaultof<_>
                    then None
                    else Some ()

                match! dbContext.FindAsync id with
                | DefaultSubmission -> return Error [SubmissionDoesNotExist id]
                | submission -> return Ok submission
            }

        member this.Update id items zipCode =
            task {
                let! submission = % (this :> ISubmissionService).Get id
                let! items = % makeItemBatch itemService items
                let result = dbContext.Submissions.Update {submission with Items = items; ZipCode = zipCode}
                return Ok result.Entity
            }
