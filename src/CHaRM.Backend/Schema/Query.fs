[<AutoOpen>]
module CHaRM.Backend.Schema.Query

open System
open FSharp.Utils.Tasks
open GraphQL.FSharp
open GraphQL.FSharp.Builder
open GraphQL.FSharp.Server
open Validation.Builder

open CHaRM.Backend.Schema.Utils
open CHaRM.Backend.Services

let Item (items: IItemService) =
    endpoints [
        endpoint "All" __ [
            description "List of items available to submit"

            resolve.endpoint (fun _ -> task { return! items.All () })
        ]

        endpoint "Single" __ [
            description "A single item identified by its GUID"
            argumentDocumentation [
                "Id" => "The GUID of the item"
            ]

            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> task { return! items.Get args.Id })
        ]
    ]

let Submission (submissions: ISubmissionService) =
    endpoints [
        endpoint "All" __ [
            description "List all submissions in the system"

            authorize Employee
            resolve.endpoint (fun _ -> task { return! submissions.All () })
        ]

        endpoint "Get" __ [
            description "A single submission identified by its GUID"
            argumentDocumentation [
                "Id" => "The GUID of the submission"
            ]

            authorize Employee
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> task { return! submissions.Get args.Id })
        ]

        endpoint "AllMine" __ [
            description "List of all submissions by the current user"

            authorize Visitor
            resolve.endpoint (fun _ -> task { return! submissions.All () })
        ]

        endpoint "GetMine" __ [
            description "A single submission by the current user identified by its GUID"
            argumentDocumentation [
                "Id" => "The GUID of the submission"
            ]

            authorize Visitor
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> task { return! submissions.Get args.Id })
        ]
    ]

let User (users: IUserService) =
    endpoints [
        endpoint "Me" __ [
            description "The current user"

            authorize LoggedIn
            resolve.endpoint (fun _ -> task { return! users.Me () })
        ]
    ]
