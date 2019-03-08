module CHaRM.Backend.Schema

open System
open System.Security.Claims
open System.Threading.Tasks
open FSharp.Utils
open GraphQL.FSharp
open GraphQL.FSharp.Server
open Microsoft.AspNetCore.Authorization
open Validation.Builder

open CHaRM.Backend.Model
open CHaRM.Backend.Services

let (|Nullable|) x = Option.ofNullObj x

type AuthorizationPolicyBuilder with
    member this.RequireUserType (``type``: UserType) =
        this.RequireClaim (ClaimTypes.Role, Enum.GetName (typeof<UserType>, ``type``))

type Policy =
    | LoggedIn
    | Visitor
    | Employee
    | Administrator

    interface IPolicy with
        member this.Authorize builder =
            match this with
            | LoggedIn ->
                builder
                    .RequireAuthenticatedUser()
            | Visitor ->
                builder
                    .RequireAuthenticatedUser()
                    .RequireUserType(UserType.Visitor)
            | Employee ->
                builder
                    .RequireAuthenticatedUser()
                    .RequireUserType(UserType.Employee)
            | Administrator ->
                builder
                    .RequireAuthenticatedUser()
                    .RequireUserType(UserType.Administrator)

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

let UserTypeGraph = enum.auto<UserType> ()

let UserGraph =
    object<User> {
        name "User"
        fields [
            field UserTypeGraph {
                prop (fun this -> Task.FromResult this.Type)
            }
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

let SubmissionGraph =
    object<Submission> {
        fields [
            field __ {
                prop (fun this -> Task.FromResult this.Id)
            }
            field __ {
                prop (fun this -> Task.FromResult this.Visitor)
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

let Query (items: IItemService) (submissions: ISubmissionService) (users: IUserService) =
    query [
        endpoint __ "Items" {
            description "List of items available to submit"
            resolve (fun _ _ -> items.All ())
        }

        endpoint __ "Item" {
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> items.Get args.Id)
        }

        endpoint __ "Submissions" {
            authorize Visitor
            resolve (fun _ _ -> submissions.All ())
        }

        endpoint __ "Submission" {
            authorize Visitor
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> submissions.Get args.Id)
        }

        endpoint __ "Me" {
            authorize LoggedIn
            resolve (fun _ _ -> users.Me ())
        }

        endpoint __ "AllSubmissions" {
            authorize Employee
            resolve (fun _ _ -> submissions.All ())
        }

        endpoint __ "GetSubmission" {
            authorize Employee
            validate (
                fun (args: {|Id: Guid|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> submissions.Get args.Id)
        }
    ]

// TODO: Add submission log ability for guests?
let Mutation (items: IItemService) (submissions: ISubmissionService) (users: IUserService) =
    mutation [
        endpoint __ "CreateItem" {
            validate (
                fun (args: {|Name: string|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> items.Create args.Name)
        }

        endpoint __ "CreateSubmission" {
            validate (
                fun (args: {|Items: Guid []; ZipCode: string|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> submissions.Create args.Items args.ZipCode)
        }

        endpoint __ "Login" {
            validate (
                fun (args: {|Username: string; Password: string|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> users.Login args.Username args.Password)
        }

        // TODO: Fix issue with getting failure after the first mistake
        endpoint __ "Register" {
            validate (
                fun (args: {|Username: string; Email: string; Password: string|}) -> validation {
                    return args
                }
            )
            resolve (fun _ args -> users.Register args.Username args.Password args.Email)
        }

        endpoint __ "ModifySubmission" {
            authorize Employee
            validate (
                fun (args: {|Id: Guid; Time: DateTimeOffset; Items: Guid []|}) -> validation {
                    return args
                }
            )
            // TODO: Fix Time and zipCode
            resolve (fun _ args -> submissions.Update args.Id args.Items "")
        }

        endpoint __ "RemoveSubmission" {
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
