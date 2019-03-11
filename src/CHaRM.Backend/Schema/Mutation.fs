[<AutoOpen>]
module CHaRM.Backend.Schema.Mutation

open System
open GraphQL.FSharp
open GraphQL.FSharp.Server
open Validation.Builder

open CHaRM.Backend.Schema.Utils
open CHaRM.Backend.Services

let Item (items: IItemService) =
    // TODO: Add a way to add item descriptions for tooltips
    mutation "Item" [
        endpoint __ "Create" {
            description "Adds a new item that can be submitted"
            argumentDescription [
                "Name" => "The name of the item"
            ]

            validate (
                fun (args: {|Name: string|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> items.Create args.Name)
        }
    ]

let Submission (submissions: ISubmissionService) =
    mutation "Submission" [
        endpoint __ "CreateSelf" {
            description "Adds a new submission for the current user"
            argumentDescription [
                "Items" => "The list of the GUIDs of the items being submitted"
                "ZipCode" => "The zip code of the visitor"
            ]

            authorize Visitor
            validate (
                fun (args: {|Items: Guid []; ZipCode: string|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> submissions.Create args.Items args.ZipCode)
        }

        endpoint __ "Modify" {
            authorize Employee
            description "Modifies the contents of an existing submission"
            argumentDescription [
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
            resolve (fun _ args -> submissions.Update args.Id args.Items "")
        }

        endpoint __ "Remove" {
            description "Removes an existing submission"
            argumentDescription [
                "Id" => "The Id of the submission"
            ]

            authorize Employee
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            // TODO: Fix Time and zipCode
            resolve (fun _ args -> submissions.Delete args.Id)
        }
    ]

let User (users: IUserService) =
    mutation "User" [
        endpoint __ "Login" {
            description "Attempts to login with the provided username and password and returns a JSON web token (JWT) on success."
            argumentDescription [
                "Username" => "The user's uesrname"
                "Password" => "The user's password"
            ]

            validate (
                fun (args: {|Username: string; Password: string|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> users.Login args.Username args.Password)
        }

        // TODO: Fix issue with getting failure after the first mistake
        endpoint __ "Register" {
            description "Attempts to register with the provided information and returns a JSON web token (JWT) on success."
            argumentDescription [
                "Username" => "The user's username"
                "Email" => "The user's email"
                "Password" => "The user's password"
            ]

            validate (
                fun (args: {|Username: string; Email: string; Password: string|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> users.Register args.Username args.Password args.Email)
        }
    ]
