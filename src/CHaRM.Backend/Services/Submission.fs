[<AutoOpen>]
module CHaRM.Backend.Services.Submission

open System
open System.Threading.Tasks
open FSharp.Utils
open FSharp.Utils.Tasks
open Validation

open CHaRM.Backend.Error
open CHaRM.Backend.Model

let mutable submissions = [||]

type ISubmissionService =
    abstract member All: unit -> Submission [] Task
    abstract member Create: items: Guid [] -> zipCode: string -> Result<Submission, ErrorCode list> Task
    abstract member Delete: id: Guid -> Submission option Task
    abstract member Get: id: Guid -> Submission option Task
    abstract member Update: id: Guid -> items: Guid [] -> zipCode: string -> Result<Submission, ErrorCode list> Task

let create (itemService: IItemService) (userService: IUserService) (itemId: Guid) items zipCode =
    task {
        let! visitor =
            userService.Me ()
            |> Task.map (Validation.ofOption [NotLoggedIn])
            |> AsyncResult

        let! allItems = itemService.All ()

        let items =
            items
            |> Array.groupBy id
            |> Array.map (fun (id, ids) ->
                {
                    Id = Guid.NewGuid ()
                    Item = allItems |> Array.find (fun item -> item.Id = id)
                    Count = Array.length ids
                })
        let item = {
            Id = itemId
            Visitor = visitor
            Submitted = DateTimeOffset.Now
            Items = items
            ZipCode = zipCode
        }
        submissions <- [|yield item; yield! submissions|]
        return Ok item
    }

type SubmissionService (itemService, userService) =
    let create = create itemService userService

    interface ISubmissionService with
        member __.All () = Task.FromResult submissions

        member __.Create items zipCode = create (Guid.NewGuid ()) items zipCode

        member __.Delete id =
            task {
                return
                    submissions
                    |> Array.tryFind (fun submission -> submission.Id = id)
                    |> Option.map (fun submission -> submissions <- submissions |> Array.filter (fun submission' -> submission.Id = submission'.Id); submission)
            }

        member __.Get id =
            submissions
            |> Array.tryFind (fun submission -> submission.Id = id)
            |> Task.FromResult

        member this.Update id items zipCode =
            task {
                match! (this :> ISubmissionService).Get id with
                | Some item ->
                    let! _ = (this :> ISubmissionService).Delete item.Id
                    ()
                | _ -> ()
                return! create id items zipCode
            }
