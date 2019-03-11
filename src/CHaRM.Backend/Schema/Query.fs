[<AutoOpen>]
module CHaRM.Backend.Schema.Query

open System
open GraphQL.FSharp
open GraphQL.FSharp.Server
open Validation.Builder

open CHaRM.Backend.Schema.Utils
open CHaRM.Backend.Services

let Item (items: IItemService) =
    query "Item" [
        endpoint __ "All" {
            description "List of items available to submit"
            resolve (fun _ _ -> items.All ())
        }

        endpoint __ "Single" {
            description "A single item identified by its GUID"
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> items.Get args.Id)
        }
    ]

let Submission (submissions: ISubmissionService) =
    query "Submission" [
        endpoint __ "All" {
            description "List all submissions in the system"
            authorize Employee
            resolve (fun _ _ -> submissions.All ())
        }

        endpoint __ "Get" {
            description "A single submission identified by its GUID"
            authorize Employee
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> submissions.Get args.Id)
        }

        endpoint __ "AllMine" {
            description "List of all submissions by the current user"
            authorize Visitor
            resolve (fun _ _ -> submissions.All ())
        }

        endpoint __ "GetMine" {
            description "A single submission by the current user identified by its GUID"
            authorize Visitor
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> submissions.Get args.Id)
        }
    ]

let User (users: IUserService) =
    query "User" [
        endpoint __ "Me" {
            description "The current user"
            authorize LoggedIn
            resolve (fun _ _ -> users.Me ())
        }
    ]
