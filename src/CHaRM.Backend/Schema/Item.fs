module CHaRM.Backend.Schema.Item

open System
open FSharp.Utils.Tasks
open GraphQL.FSharp
open GraphQL.FSharp.Builder
open Validation.Builder

open CHaRM.Backend.Model
open CHaRM.Backend.Services

[<AutoOpen>]
module Arguments =
    module Query =
        type Item = {Id: Guid}
    module Mutation =
        type CreateItem = {Name: string; Description: string}
        type ModifyItem = {Id: Guid; Name: string option; Description: string option}

let ItemTypeGraph =
    object<ItemType> [
        Documentation.description "A type that represents a specific acceptable item in our database."
        fields [
            field __ [
                Documentation.description "The item's unique GUID"
                resolve.property (fun this -> vtask { return this.Id })
            ]

            field __ [
                Documentation.description "The item's name"
                resolve.property (fun this -> vtask { return this.Name })
            ]

            field __ [
                Documentation.description "The item's description"
                resolve.property (fun this -> vtask { return this.Description })
            ]
        ]
    ]

let Query (items: IItemService) =
    endpoints [
        endpoint "Items" __ [
            Documentation.description "List of items available to submit"

            resolve.endpoint (fun _ -> vtask { return! items.All () })
        ]

        endpoint "Item" __ [
            Documentation.description "A single item identified by its GUID"
            Documentation.arguments [
                "Id" => "The GUID of the item"
            ]

            validate (
                fun (args: Query.Item) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! items.Get args.Id })
        ]
    ]

let Mutation (items: IItemService) =
    // TODO: Add a way to add item descriptions for tooltips
    endpoints [
        endpoint "CreateItem" __ [
            Documentation.description "Adds a new item that can be submitted"
            Documentation.arguments [
                "Name" => "The name of the item"
                "Description" => "The description for the item"
            ]

            validate (
                fun (args: Mutation.CreateItem) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! items.Create args.Name args.Description })
        ]
        endpoint "ModifyItem" __ [
            Documentation.description "Modifies an existing item that can be submitted"
            Documentation.arguments [
                "Id" => "The id of the item"
                "Name" => "The new name of the item"
                "Description" => "The new description for the item"
            ]

            validate (
                fun (args: Mutation.ModifyItem) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! items.Edit args.Id args.Name args.Description })
        ]
    ]

let Types: Types = [
    ItemTypeGraph
]
