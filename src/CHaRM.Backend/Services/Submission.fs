[<AutoOpen>]
module CHaRM.Backend.Services.Submission

open System
open System.IO
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open FSharp.Utils
open FSharp.Utils.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Primitives

open CHaRM.Backend.Database
open CHaRM.Backend.Error
open CHaRM.Backend.Model
open CHaRM.Backend.Util

// TODO: Add proper pagination

exception ItemNotFoundException of Id: Guid

type ISubmissionService =
    abstract member All: unit -> Submission [] ValueTask
    abstract member AllOf: userId: Guid -> Submission [] ValueTask
    abstract member Create: visitor: User -> items: Guid [] -> zipCode: string -> Result<Submission, ErrorCode list> ValueTask
    abstract member Delete: id: Guid -> Result<Submission, ErrorCode list> ValueTask
    abstract member Get: id: Guid -> Result<Submission, ErrorCode list> ValueTask
    abstract member Update: id: Guid -> items: Guid [] -> zipCode: string -> Result<Submission, ErrorCode list> ValueTask
    abstract member DownloadExcel: startDate: DateTimeOffset -> endDate: DateTimeOffset -> struct (MemoryStream * string) ValueTask

// TODO: Move to utils
module Option =
    let getOrRaiseWith (f: unit -> #exn) option =
        match option with
        | Some value -> value
        | None -> raise (f ())

let makeItemBatch (itemService: IItemService) items submissionId =
    vtask {
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
    vtask {
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
    vtask {
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
    vtask {
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
    vtask {
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
    vtask {
        let! submission = % get db id
        let result = db.Submissions.Remove submission
        return Ok result.Entity
    }

let update (db: ApplicationDbContext) itemService id items zipCode =
    vtask {
        let! submission = % get db id
        let! items = % makeItemBatch itemService items id
        submission.Items <- items
        submission.ZipCode <- zipCode
        let result = db.Submissions.Update submission
        // let result = db.Submissions.Update {submission with Items = items; ZipCode = zipCode}
        return Ok result.Entity
    }

open OfficeOpenXml
[<RequireQualifiedAccess>]
module Queryable =
    let toArray (queryable: IQueryable<_>) = queryable.ToArrayAsync ()

let downloadExcel (db: ApplicationDbContext) startDate endDate =
    vtask {
        use package = new ExcelPackage ()
        let worksheet = package.Workbook.Worksheets.Add "Submissions"

        let! data =
            query {
                for submission in db.Submissions do
                    where (submission.Submitted >= startDate && submission.Submitted <= endDate)
                    select submission
            }
            |> Queryable.toArray

        worksheet.Cells.["B2:B2"].LoadFromCollection data
        |> ignore

        let stream = new MemoryStream ()
        package.SaveAs stream
        return struct (stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
    }

type SubmissionService (db, itemService) =
    let all () = all db
    let allOf id = allOf db id
    let create visitor items zipCode = create db itemService visitor items zipCode
    let get id = get db id
    let delete id = delete db id
    let update id items zipCode = update db itemService id items zipCode
    let downloadExcel startDate endDate = downloadExcel db startDate endDate

    interface ISubmissionService with
        member __.All () = vtask { return! all () }
        member __.AllOf id = vtask { return! allOf id }
        member __.Create visitor items zipCode = db.changes { return! create visitor items zipCode }
        member __.Delete id = db.changes { return! delete id }
        member __.Get id = get id
        member __.Update id items zipCode = db.changes { return! update id items zipCode }
        member __.DownloadExcel startDate endDate = downloadExcel startDate endDate
