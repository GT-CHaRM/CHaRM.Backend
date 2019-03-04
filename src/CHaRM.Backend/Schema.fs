module CHaRM.Backend.Schema

open System
open System.Threading.Tasks
open GraphQL.FSharp
open GraphQL.FSharp.Server
open Microsoft.AspNetCore.Authorization

open CHaRM.Backend.Model
open CHaRM.Backend.Services

type Role =
    | LoggedIn
    | Visitor
    | Employee
    | Administrator

    member this.Authorize (_services: IServiceProvider, builder: AuthorizationPolicyBuilder) =
        match this with
        | LoggedIn -> builder
        | Visitor -> builder
        | Employee -> builder
        | Administrator -> builder


let ItemTypeGraph =
    object<ItemType> {
        fields [
            field __ {
                prop (fun this -> Task.FromResult this.Id)
            }
            field __ {
                prop (fun this -> Task.FromResult this.Name)
            }
        ]
    }

let ItemSubmissionBatchGraph =
    object<ItemSubmissionBatch> {
        fields [
            field __ {
                prop (fun this -> Task.FromResult this.Id)
            }
            field __ {
                prop (fun this -> Task.FromResult this.Count)
            }
            field __ {
                prop (fun this -> Task.FromResult this.Item)
            }
        ]
    }

let SubmissionGraph =
    object<Submission> {
        fields [
            field __ {
                prop (fun this -> Task.FromResult this.Id)
            }
            field __ {
                prop (fun this -> Task.FromResult this.Items)
            }
            field __ {
                prop (fun this -> Task.FromResult this.Submitted)
            }
            field __ {
                prop (fun this -> Task.FromResult (Option.ofObj this.ZipCode))
            }
        ]
    }

let UserGraph =
    object<User> {
        name "User"
        fields [
            field __ {
                prop (fun this -> Task.FromResult this.UserName)
            }
            field __ {
                prop (fun this -> Task.FromResult this.NormalizedEmail)
            }
            field __ {
                prop (fun this -> Task.FromResult this.Submissions)
            }
            field __ {
                prop (fun this -> Task.FromResult (Option.ofObj this.ZipCode))
            }
        ]
    }

let Query (items: IItemService) (submissions: ISubmissionService) (users: IUserService) =
    query [
        endpoint __ "Items" {
            authorize Visitor
            description "List of items available to submit"
            resolve (
                fun _ _ ->
                    items.All ()
            )
        }

        endpoint __ "Item" {
            authorize Visitor
            resolve (
                fun _ args ->
                    items.Get args
            )
        }

        endpoint __ "Submissions" {
            authorize Visitor
            resolve (
                fun _ _ -> submissions.All ()
            )
        }

        endpoint __ "Submission" {
            authorize Visitor
            resolve (
                fun _ args ->
                    submissions.Get args
            )
        }

        endpoint __ "Me" {
            authorize LoggedIn
            resolve (
                fun _ _ ->
                    users.Me ()
            )
        }
    ]


let Mutation (items: IItemService) (submissions: ISubmissionService) (users: IUserService) =
    mutation [
        endpoint __ "CreateItem" {
            resolve (
                fun _ args ->
                    items.Create args
            )
        }

        endpoint __ "CreateSubmission" {
            authorize Visitor
            resolve (
                fun _ args ->
                    submissions.Create args
            )
        }

        endpoint __ "Login" {
            resolve (
                fun _ (args: {|Username: string; Password: string|}) ->
                    users.Login args.Username args.Password
            )
        }

        // TODO: Fix issue with getting failure after the first mistake
        endpoint __ "Register" {
            resolve (
                fun _ (args: {|Username: string; Email: string; Password: string|}) ->
                    users.Register args.Username args.Password args.Email
            )
        }
    ]

let Schema (items: IItemService, submissions: ISubmissionService, users: IUserService) =
    schema {
        query (Query items submissions users)
        mutation (Mutation items submissions users)
        types [
            ItemTypeGraph
            ItemSubmissionBatchGraph
            SubmissionGraph
            UserGraph
        ]
    }
