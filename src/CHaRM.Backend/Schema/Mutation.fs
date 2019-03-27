[<AutoOpen>]
module CHaRM.Backend.Schema.Mutation

open System
open FSharp.Utils.Tasks
open GraphQL.FSharp
open GraphQL.FSharp.Builder
open GraphQL.FSharp.Server
open Validation.Builder

open CHaRM.Backend.Schema.Utils
open CHaRM.Backend.Services

let Item (items: IItemService) =
    // TODO: Add a way to add item descriptions for tooltips
    endpoints [
        endpoint "CreateItem" __ [
            description "Adds a new item that can be submitted"
            argumentDocumentation [
                "Name" => "The name of the item"
            ]

            validate (
                fun (args: {|Name: string|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> task { return! items.Create args.Name })
        ]
    ]

let Submission (users: IUserService) (submissions: ISubmissionService) =
    endpoints [
        endpoint "CreateSubmissionSelf" __ [
            description "Adds a new submission for the current user"
            argumentDocumentation [
                "Items" => "The list of the GUIDs of the items being submitted"
                "ZipCode" => "The zip code of the visitor"
            ]

            // authorize Visitor
            validate (
                fun (args: {|Items: Guid []; ZipCode: string|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (
                fun args -> task {
                    let! me = % users.Me ()
                    return! submissions.Create me args.Items args.ZipCode
                }
            )
        ]

        endpoint "ModifySubmission" __ [
            authorize Employee
            description "Modifies the contents of an existing submission"
            argumentDocumentation [
                "Id" => "The Id of the initial submission"
                "Items" => "The new list of the GUIDs for the submission"
                "Time" => "The new time of submission"
                "ZipCode" => "The new zip code of the visitor for the submission"
            ]

            validate (
                fun (args: {|Id: Guid; Time: DateTimeOffset; Items: Guid []; ZipCode: string|}) -> validation {
                    return args
                }
            )
            // TODO: Fix Time and zipCode
            resolve.endpoint (fun args -> task { return! submissions.Update args.Id args.Items "" })
        ]

        endpoint "RemoveSubmission" __ [
            description "Removes an existing submission"
            argumentDocumentation [
                "Id" => "The Id of the submission"
            ]

            authorize Employee
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            // TODO: Fix Time and zipCode
            resolve.endpoint (fun args -> task { return! submissions.Delete args.Id })
        ]
    ]

let User (users: IUserService) =
    endpoints [
        endpoint "LoginUser" __ [
            description "Attempts to login with the provided username and password and returns a JSON web token (JWT) on success."
            argumentDocumentation [
                "Username" => "The user's uesrname"
                "Password" => "The user's password"
            ]

            validate (
                fun (args: {|Username: string; Password: string|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> task { return! users.Login args.Username args.Password })
        ]

        // TODO: Fix issue with getting failure after the first mistake
        endpoint "RegisterUser" __ [
            description "Attempts to register with the provided information and returns a JSON web token (JWT) on success."
            argumentDocumentation [
                "Username" => "The user's username"
                "Email" => "The user's email"
                "Password" => "The user's password"
            ]

            validate (
                fun (args: {|Username: string; Email: string; Password: string|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> task { return! users.Register args.Username args.Password args.Email })
        ]
    ]
