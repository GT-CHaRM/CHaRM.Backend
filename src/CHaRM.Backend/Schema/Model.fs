[<AutoOpen>]
module CHaRM.Backend.Schema.Model

open System.Threading.Tasks
open FSharp.Utils
open FSharp.Utils.Tasks
open GraphQL.FSharp
open GraphQL.FSharp.Builder

open CHaRM.Backend.Model

let ItemTypeGraph =
    object<ItemType> [
        description "A type that represents a specific acceptable item in our database."
        fields [
            field __ [
                description "The item's unique GUID"
                resolve.property (fun this -> task { return this.Id })
            ]

            field __ [
                description "The item's name"
                resolve.property (fun this -> task { return this.Name })
            ]
        ]
    ]

let ItemSubmissionBatchGraph =
    object<ItemSubmissionBatch> [
        description "A type that represents a unique submission for a specific item, including the item id and the count submitted."
        fields [
            field __ [
                description "The item batch's unique GUID"
                resolve.property (fun this -> task { return this.Id })
            ]

            field __ [
                description "The count of the item that was submitted."
                resolve.property (fun this -> task { return this.Count })
            ]

            field __ [
                description "The item submitted"
                resolve.property (fun this -> task { return this.Item })
            ]
        ]
    ]

let UserTypeGraph =
    enum<UserType> [
        description "A specific type of user"
        cases [
            case UserType.Visitor []
            case UserType.Employee []
            case UserType.Administrator []
        ]
    ]

let UserGraph =
    object<User> [
        description "A user registered with CHaRM"
        fields [
            field UserTypeGraph [
                description "The type of the user"
                resolve.property (fun this -> task { return this.Type })
            ]

            field __ [
                description "The user's unique username"
                resolve.property (fun this -> task { return this.UserName })
            ]

            field __ [
                description "The user's email"
                resolve.property (fun this -> task { return this.Email })
            ]

            field __ [
                description "The user's zip code"
                resolve.property (fun this -> task { return (Option.ofObj this.ZipCode) })
            ]
        ]
    ]

let SubmissionGraph =
    object<Submission> [
        description "The list of items submitted in a single visit to CHaRM"
        fields [
            field __ [
                description "The unique id of this submission"
                resolve.property (fun this -> task { return this.Id })
            ]

            field __ [
                description "The visitor who performed the submission"
                resolve.property (fun this -> task { return this.Visitor })
            ]

            field __ [
                description "The list of items (+ counts) submitted"
                resolve.property (fun this -> task { return this.Items })
            ]

            field __ [
                description "The date of submission"
                resolve.property (fun this -> task { return this.Submitted })
            ]

            field __ [
                description "The zip code of the visitor who performed the submission."
                resolve.property (fun this -> task { return (Option.ofObj this.ZipCode) })
            ]
        ]
    ]
