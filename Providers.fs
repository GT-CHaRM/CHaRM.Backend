module CHaRM.Providers

open System
open System.Collections.Generic
open Model
open Util

type UserProvider = {
    All: unit -> User obs
    Get: Guid -> User obs
    // Login: string -> string -> User obs
    // Register: string -> string -> User obs
}

type ItemProvider = {
    All: unit -> ItemType obs
    Create: string -> ItemType obs
    Get: Guid -> ItemType obs
}

type SubmissionProvider = {
    All: unit -> Submission obs
    Create: Guid List -> Submission obs
    Get: Guid -> Submission obs
}

module Mock =
    open System
    open System.Collections.Generic
    open FSharp.Control.Reactive
    open FSharp.Control.Reactive.Builders
    open Microsoft.Extensions.DependencyInjection

    open Model
    open Iris

    open Iris.Observable.Operators


    module Service =
        let addSingleton<'t when 't : not struct> (singleton: 't) (services: IServiceCollection) = services.AddSingleton<'t> singleton

    let visitorWithSubmissions =
        let submissions = List<Submission>()
        submissions.Add({Id = Guid.NewGuid(); Submitted = DateTimeOffset.Now; Items = List(); })
        Visitor(Id = Guid.NewGuid(), Name = "two", Submissions = submissions)

    let allUsers = [
        Visitor(Id = Guid.NewGuid(), Name = "sup") :> User
        visitorWithSubmissions :> User
    ]

    let getAllUsers () = Observable.ofSeq allUsers

    let getUser id = rxquery {
        for user in getAllUsers () do
            where (user.Id = id)
            take 1
    }

    module Items =
        let internal items = List<ItemType>()
        let all () = Observable.ofSeq items
        let get id = all () |> Observable.get (ItemType.id >> (=) id)
        let create name =
            let item = {Id = Guid.NewGuid (); Name = name}
            items.Add item
            Observable.lift item

        create "Appliances (Large)" |> ignore
        create "Appliances (Small)" |> ignore
        create "Batteries" |> ignore
        create "Bulbs" |> ignore

    module Submissions =
        let internal submissions = List<Submission>()
        let all () = Observable.ofSeq submissions
        let get id = all () |> Observable.get (Submission.id >> (=) id)
        let create (items: Guid seq) =
            items
            |> Seq.map Items.get
            |> Observable.combineLatestSeq
            <*> Seq.groupBy ItemType.id
            <*> Seq.map (fun (id, items) ->
                let item = items |> Seq.head
                ItemInput.create id (Seq.length items) item.Name)
            |> Observable.map (fun items -> {Id = Guid.NewGuid(); Submitted = DateTimeOffset.Now; Items = List(items)})
            |> Observable.perform submissions.Add

    let register (services: IServiceCollection) =
        services
        |> Service.addSingleton<ItemProvider> {
            All = Items.all;
            Create = Items.create;
            Get = Items.get;
        }
        |> Service.addSingleton<UserProvider> {
            All = getAllUsers;
            Get = getUser;
        }
        |> Service.addSingleton<SubmissionProvider> {
            All = Submissions.all;
            Create = Submissions.create;
            Get = Submissions.get;
        }
