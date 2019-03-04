[<AutoOpen>]
module CHaRM.Backend.Provider.Submission

open System
open System.Threading.Tasks

open CHaRM.Backend.Model

let mutable submissions = [||]

type SubmissionProvider = {
    All: unit -> Submission [] Task
    Create: {|Items: Guid []; ZipCode: string|} -> Submission Task
    Get: {|Id: Guid|} -> Submission Task
}

let submissionProvider: SubmissionProvider = {
    All = fun () -> Task.FromResult submissions
    Create = fun args ->
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
    Get = fun args ->
        submissions
        |> Array.find (fun {Id = id} -> args.Id = id)
        |> Task.FromResult
}
