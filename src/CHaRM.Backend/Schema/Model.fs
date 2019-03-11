[<AutoOpen>]
module CHaRM.Backend.Schema.Model

open System.Threading.Tasks
open FSharp.Utils
open GraphQL.FSharp

open CHaRM.Backend.Model

let ItemTypeGraph =
    object<ItemType> {
        description "A type that represents a specific acceptable item in our database."
        fields [
            field __ {
                description "The item's unique GUID"
                prop (fun this -> Task.FromResult this.Id)
            }

            field __ {
                description "The item's name"
                prop (fun this -> Task.FromResult this.Name)
            }
        ]
    }

let ItemSubmissionBatchGraph =
    object<ItemSubmissionBatch> {
        description "A type that represents a unique submission for a specific item, including the item id and the count submitted."
        fields [
            field __ {
                description "The item batch's unique GUID"
                prop (fun this -> Task.FromResult this.Id)
            }

            field __ {
                description "The count of the item that was submitted."
                prop (fun this -> Task.FromResult this.Count)
            }

            field __ {
                description "The item submitted"
                prop (fun this -> Task.FromResult this.Item)
            }
        ]
    }

let UserTypeGraph =
    enum.auto<UserType> (
        Description = "A specific type of user"
    )

let UserGraph =
    object<User> {
        description "A user registered with CHaRM"
        fields [
            field UserTypeGraph {
                description "The type of the user"
                prop (fun this -> Task.FromResult this.Type)
            }

            field __ {
                description "The user's unique username"
                prop (fun this -> Task.FromResult this.UserName)
            }

            field __ {
                description "The user's email"
                prop (fun this -> Task.FromResult this.Email)
            }

            field __ {
                description "The user's zip code"
                prop (fun this -> Task.FromResult (Option.ofObj this.ZipCode))
            }
        ]
    }

let SubmissionGraph =
    object<Submission> {
        description "The list of items submitted in a single visit to CHaRM"
        fields [
            field __ {
                description "The unique id of this submission"
                prop (fun this -> Task.FromResult this.Id)
            }

            field __ {
                description "The visitor who performed the submission"
                prop (fun this -> Task.FromResult this.Visitor)
            }

            field __ {
                description "The list of items (+ counts) submitted"
                prop (fun this -> Task.FromResult this.Items)
            }

            field __ {
                description "The date of submission"
                prop (fun this -> Task.FromResult this.Submitted)
            }

            field __ {
                description "The zip code of the visitor who performed the submission."
                prop (fun this -> Task.FromResult (Option.ofObj this.ZipCode))
            }
        ]
    }
