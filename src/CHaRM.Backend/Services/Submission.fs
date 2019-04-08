[<AutoOpen>]
module CHaRM.Backend.Services.Submission

open System
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Utils
open FSharp.Utils.Tasks
open Microsoft.EntityFrameworkCore

open CHaRM.Backend.Database
open CHaRM.Backend.Error
open CHaRM.Backend.Model
open CHaRM.Backend.Util

// TODO: Add proper pagination

exception ItemNotFoundException of Id: Guid

type ISubmissionService =
    abstract member All: unit -> Submission [] Task
    abstract member AllOf: userId: Guid -> Submission [] Task
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

let makeItemBatch (itemService: IItemService) items submissionId =
    task {
        let! allItems = itemService.All ()
        try
            return
                items
                |> Array.groupBy id
                |> Array.map (
                    fun (id, group) -> {
                        Id = Guid.NewGuid ()
                        Item =
                            allItems
                            |> Array.tryFind (fun item -> item.Id = id)
                            |> Option.getOrRaiseWith (fun () -> raise (ItemNotFoundException id))
                        Count = Array.length group
                        SubmissionId = submissionId
                    }
                )
                |> HashSet
                |> Ok
        with :? ItemNotFoundException as exn -> return Error [ItemNotFound exn.Id]
    }

let all (db: ApplicationDbContext) =
    task {
        let! submissions =
            db.Submissions
                .Include(fun submission -> submission.Items :> _ seq) // we have to upcast beacuse F# doesn't auto upcast
                .ThenInclude(fun item -> item.Item)
                .ToArrayAsync()
        return
            submissions
            |> Array.sortByDescending (fun submission -> submission.Submitted)
    }

let allOf (db: ApplicationDbContext) id =
    task {
        let! submissions =
            (query {
                for submission in db.Submissions do
                    where (submission.Visitor.Id = id)
                    select submission
            })
                .Include(fun submission -> submission.Items :> _ seq) // we have to upcast beacuse F# doesn't auto upcast
                .ThenInclude(fun item -> item.Item)
                .ToArrayAsync()
        return
            submissions
            |> Array.sortByDescending (fun submission -> submission.Submitted)
    }

let create (db: ApplicationDbContext) itemService visitor items zipCode =
    task {
        let id = Guid.NewGuid ()
        let! items = % makeItemBatch itemService items id
        let! result =
            db.Submissions.AddAsync {
                Id = id
                Visitor = visitor
                Submitted = DateTimeOffset.Now
                Items = items
                ZipCode = zipCode
            }
        return Ok result.Entity
    }

let get (db: ApplicationDbContext) (id: Guid) =
    task {
        let! submission =
            db
                .Submissions
                .Include(fun submission -> submission.Items :> _ seq) // we have to upcast beacuse F# doesn't auto upcast
                .ThenInclude(fun item -> item.Item)
                .SingleOrDefaultAsync(fun submission -> submission.Id = id)
        match submission with
        | Default -> return Error [SubmissionDoesNotExist id]
        | submission -> return Ok submission
    }

let delete (db: ApplicationDbContext) id =
    task {
        let! submission = % get db id
        let result = db.Submissions.Remove submission
        return Ok result.Entity
    }

let update (db: ApplicationDbContext) itemService id items zipCode =
    task {
        let! submission = % get db id
        let! items = % makeItemBatch itemService items id
        submission.Items <- items
        submission.ZipCode <- zipCode
        let result = db.Submissions.Update submission
        // let result = db.Submissions.Update {submission with Items = items; ZipCode = zipCode}
        return Ok result.Entity
    }

type SubmissionService (db, itemService) =
    let all () = all db
    let allOf id = allOf db id
    let create visitor items zipCode = create db itemService visitor items zipCode
    let get id = get db id
    let delete id = delete db id
    let update id items zipCode = update db itemService id items zipCode

    interface ISubmissionService with
        member __.All () = task { return! all () }
        member __.AllOf id = task { return! allOf id }
        member __.Create visitor items zipCode = db.changes { return! create visitor items zipCode }
        member __.Delete id = db.changes { return! delete id }
        member __.Get id = get id
        member __.Update id items zipCode = db.changes { return! update id items zipCode }
