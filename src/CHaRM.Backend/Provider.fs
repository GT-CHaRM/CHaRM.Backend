module CHaRM.Backend.Provider

(* This module contains the interfaces (and mock implementations) of our methods that access/modify the database. *)

open System
open System.Threading.Tasks

open CHaRM.Backend.Model

/// The methods that access/modify the list of submittable items in our database.
type ItemProvider = {
    All: unit -> ItemType list Task
    Create: string -> ItemType Task
    Get: Guid -> ItemType Task
}

/// The methods that access/modify the list of submissions by visitors in our database.
type SubmissionProvider = {
    All: unit -> Submission list Task
    Create: Guid list -> Submission Task
    Get: Guid -> Submission Task
}

(* Mock implementations *)

let mutable items = [{Id = Guid.NewGuid (); Name = "myName"}]
let itemProvider: ItemProvider = {
    All = fun () -> Task.FromResult items
    Create = fun name ->
        let item = {
            Id = Guid.NewGuid ()
            Name = name
        }
        items <- item :: items
        Task.FromResult item
    Get = fun id ->
        items
        |> List.find (fun {Id = id'} -> id = id')
        |> Task.FromResult
}

let mutable submissions = [{Id = Guid.NewGuid (); Items = [{Item = items.[0]; Count = 5}]; Submitted = DateTimeOffset.Now}]
let submissionProvider: SubmissionProvider = {
    All = fun () -> Task.FromResult submissions
    Create = fun submittedItems ->
        let items =
            submittedItems
            |> List.groupBy id
            |> List.map (fun (id, lst) ->
                {
                    Item = items |> List.find (fun item -> item.Id = id)
                    Count = List.length lst
                })
        let item = {
            Id = Guid.NewGuid ()
            Submitted = DateTimeOffset.Now
            Items = items
        }
        submissions <- item :: submissions
        Task.FromResult item
    Get = fun id ->
        submissions
        |> List.find (fun {Id = id'} -> id = id')
        |> Task.FromResult
}
