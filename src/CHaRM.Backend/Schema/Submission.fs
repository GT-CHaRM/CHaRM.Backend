module CHaRM.Backend.Schema.Submission

open System
open FSharp.Utils.Tasks
open GraphQL.FSharp
open GraphQL.FSharp.Builder
open GraphQL.FSharp.Server
open Validation.Builder

open CHaRM.Backend.Model
open CHaRM.Backend.Services

[<AutoOpen>]
module Arguments =
    module Query =
        type Submission = {Id: Guid}
        type MySubmission = {Id: Guid}
        type AllSubmissionsOf = {UserId: Guid}
    module Mutation =
        type CreateSubmissionSelf = {Items: Guid []; ZipCode: string}
        type ModifySubmission = {Id: Guid; Time: DateTimeOffset; Items: Guid []; ZipCode: string}
        type RemoveSubmission = {Id: Guid}

let ItemSubmissionBatchGraph =
    object<ItemSubmissionBatch> [
        Documentation.description "A type that represents a unique submission for a specific item, including the item id and the count submitted."
        fields [
            field __ [
                Documentation.description "The item batch's unique GUID"
                resolve.property (fun this -> vtask { return this.Id })
            ]

            field __ [
                Documentation.description "The count of the item that was submitted."
                resolve.property (fun this -> vtask { return this.Count })
            ]

            field __ [
                Documentation.description "The item submitted"
                resolve.property (fun this -> vtask { return this.Item })
            ]
        ]
    ]


let SubmissionGraph =
    object<Submission> [
        Documentation.description "The list of items submitted in a single visit to CHaRM"
        fields [
            field __ [
                Documentation.description "The unique id of this submission"
                resolve.property (fun this -> vtask { return this.Id })
            ]

            field __ [
                Documentation.description "The visitor who performed the submission"
                resolve.property (fun this -> vtask { return this.Visitor })
            ]

            field __ [
                Documentation.description "The list of items (+ counts) submitted"
                resolve.property (fun this -> vtask { return this.Items })
            ]

            field __ [
                Documentation.description "The date of submission"
                resolve.property (fun this -> vtask { return this.Submitted })
            ]

            field __ [
                Documentation.description "The zip code of the visitor who performed the submission."
                resolve.property (fun this -> vtask { return (Option.ofObj this.ZipCode) })
            ]
        ]
    ]

let Query (submissions: ISubmissionService) =
    endpoints [
        endpoint "AllSubmissions" __ [
            Documentation.description "List all submissions in the system"

            // authorize Employee
            resolve.endpoint (fun _ -> vtask { return! submissions.All () })
        ]

        endpoint "GetAllSubmissionsFromUser" __ [
            Documentation.description "List all submissions in the system"
            validate (
                fun (args: Query.AllSubmissionsOf) -> validation {
                    return args
                }
            )

            // authorize Employee
            resolve.endpoint (fun args -> vtask { return! submissions.AllOf args.UserId })
        ]

        endpoint "Submission" __ [
            Documentation.description "A single submission identified by its GUID"
            Documentation.arguments [
                "Id" => "The GUID of the submission"
            ]

            // authorize Employee
            validate (
                fun (args: Query.Submission) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! submissions.Get args.Id })
        ]

        endpoint "MySubmissions" __ [
            Documentation.description "List of all submissions by the current user"

            authorize Visitor
            resolve.endpoint (fun _ -> vtask { return! submissions.All () })
        ]

        endpoint "MySubmission" __ [
            Documentation.description "A single submission by the current user identified by its GUID"
            Documentation.arguments [
                "Id" => "The GUID of the submission"
            ]

            authorize Visitor
            validate (
                fun (args: Query.MySubmission) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! submissions.Get args.Id })
        ]
    ]

let Mutation (users: IUserService) (submissions: ISubmissionService) =
    endpoints [
        endpoint "CreateSubmissionSelf" __ [
            Documentation.description "Adds a new submission for the current user"
            Documentation.arguments [
                "Items" => "The list of the GUIDs of the items being submitted"
                "ZipCode" => "The zip code of the visitor"
            ]

            // authorize Visitor
            validate (
                fun (args: Mutation.CreateSubmissionSelf) -> validation {
                    return args
                }
            )
            resolve.endpoint (
                fun args -> vtask {
                    let! me = % users.Me ()
                    return! submissions.Create me args.Items args.ZipCode
                }
            )
        ]

        endpoint "ModifySubmission" __ [
            //authorize Employee
            Documentation.description "Modifies the contents of an existing submission"
            Documentation.arguments [
                "Id" => "The Id of the initial submission"
                "Items" => "The new list of the GUIDs for the submission"
                "Time" => "The new time of submission"
                "ZipCode" => "The new zip code of the visitor for the submission"
            ]

            validate (
                fun (args: Mutation.ModifySubmission) -> validation {
                    return args
                }
            )
            // TODO: Fix Time and zipCode
            resolve.endpoint (fun args -> vtask { return! submissions.Update args.Id args.Items "" })
        ]

        endpoint "RemoveSubmission" __ [
            Documentation.description "Removes an existing submission"
            Documentation.arguments [
                "Id" => "The Id of the submission"
            ]

            //authorize Employee
            validate (
                fun (args: Mutation.RemoveSubmission) -> validation {
                    return args
                }
            )
            // TODO: Fix Time and zipCode
            resolve.endpoint (fun args -> vtask { return! submissions.Delete args.Id })
        ]
    ]

let Types: Types = [
    ItemSubmissionBatchGraph
    SubmissionGraph
]
