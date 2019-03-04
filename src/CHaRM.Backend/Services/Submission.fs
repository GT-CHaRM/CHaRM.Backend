[<AutoOpen>]
module CHaRM.Backend.Services.Submission

open System
open System.Threading.Tasks

open CHaRM.Backend.Model

let mutable submissions = [||]

type ISubmissionService =
    abstract member All: unit -> Submission [] Task
    abstract member Create: {|Items: Guid []; ZipCode: string|} -> Submission Task
    abstract member Get: {|Id: Guid|} -> Submission Task

type SubmissionService () =
    interface ISubmissionService with
        member __.All () = Task.FromResult submissions
        member __.Create args =
            let items =
                args.Items
                |> Array.groupBy id
                |> Array.map (fun (id, ids) ->
                    {
                        Id = Guid.NewGuid ()
                        Item = items |> Array.find (fun item -> item.Id = id)
                        Count = Array.length ids
                    })
            let item = {
                Id = Guid.NewGuid ()
                Submitted = DateTimeOffset.Now
                Items = items
                ZipCode = args.ZipCode
            }
            submissions <- [|yield item; yield! submissions|]
            Task.FromResult item
        member __.Get args =
            submissions
            |> Array.find (fun {Id = id} -> args.Id = id)
            |> Task.FromResult
