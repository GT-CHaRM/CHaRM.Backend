module CHaRM.Backend.Schema

open System
open GraphQL.FSharp
open GraphQL.FSharp.Builder
open GraphQL.FSharp.Server

open CHaRM.Backend.Model
open CHaRM.Backend.Provider
open CHaRM.Backend.Util

let ItemTypeGraph =
    object<ItemType> {
        fields [
            field {
                prop (fun this -> this.Id)
            }
            field {
                prop (fun this -> this.Name)
            }
        ]
    }

let ItemSubmissionBatchGraph =
    object<ItemSubmissionBatch> {
        fields [
            field {
                prop (fun this -> this.Id)
            }
            field {
                prop (fun this -> this.Count)
            }
            field {
                prop (fun this -> this.Item)
            }
        ]
    }

let SubmissionGraph =
    object<Submission> {
        fields [
            field {
                prop (fun this -> this.Id)
            }
            field {
                prop (fun this -> this.Items)
            }
            field {
                prop (fun this -> this.Submitted)
            }
            field {
                prop (fun this -> this.ZipCode |> Option.ofObj)
            }
        ]
    }

let UserGraph =
    object<User> {
        name "User"
        fields [
            field {
                prop (fun this -> this.UserName)
            }
            field {
                prop (fun this -> this.NormalizedEmail)
            }
            field {
                prop (fun this -> this.Submissions)
            }
            field {
                prop (fun this -> this.ZipCode |> Option.ofObj)
            }
        ]
    }

let Query (Inject (userProvider: UserProvider)) =
    query [
        endpoint "Items" {
            authorize "Visitor"
            description "List of items available to submit"
            resolveAsync (
                fun _ _ ->
                    itemProvider.All ()
            )
        }

        endpoint "Item" {
            authorize "Visitor"
            resolveAsync (
                fun _ args ->
                    itemProvider.Get args
            )
        }

        endpoint "Submissions" {
            authorize "Visitor"
            resolveAsync (
                fun _ _ -> submissionProvider.All ()
            )
        }

        endpoint "Submission" {
            authorize "Visitor"
            resolveAsync (
                fun _ args ->
                    submissionProvider.Get args
            )
        }

        // endpoint "User" {
        //     authorize "Employee"
        //     resolveAsync (
        //         fun _ (args: {|Id: Guid|}) ->
        //             userProvider.Get args.Id
        //     )
        // }

        endpoint "Me" {
            authorize "LoggedIn"
            resolveAsync (
                fun _ _ ->
                    userProvider.Me ()
            )
        }
    ]

let Mutation (Inject (userProvider: UserProvider)) =
    mutation [
        endpoint "CreateItem" {
            resolveAsync (
                fun _ args ->
                    itemProvider.Create args
            )
        }

        endpoint "CreateSubmission" {
            authorize "Visitor"
            resolveAsync (
                fun _ args ->
                    submissionProvider.Create args
            )
        }

        endpoint "Login" {
            resolveAsync (
                fun _ (args: {|Username: string; Password: string|}) ->
                    userProvider.Login args.Username args.Password
            )
        }

        // TODO: Fix issue with getting failure after the first mistake
        endpoint "Register" {
            resolveAsync (
                fun _ (args: {|Username: string; Email: string; Password: string|}) ->
                    userProvider.Register args.Username args.Password args.Email
            )
        }
    ]

let Schema (provider: IServiceProvider) =
    schema {
        query (Query provider)
        mutation (Mutation provider)
        types [
            ItemTypeGraph
            ItemSubmissionBatchGraph
            SubmissionGraph
            UserGraph
        ]
    }
