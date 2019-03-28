module CHaRM.Backend.Schema.Item

open System
open FSharp.Utils.Tasks
open GraphQL.FSharp
open GraphQL.FSharp.Builder
open Validation.Builder

open CHaRM.Backend.Model
open CHaRM.Backend.Services

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

            field __ [
                description "The item's description"
                resolve.property (fun this -> task { return this.Description })
            ]
        ]
    ]

let Query (items: IItemService) =
    endpoints [
        endpoint "Items" __ [
            description "List of items available to submit"

            resolve.endpoint (fun _ -> task { return! items.All () })
        ]

        endpoint "Item" __ [
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

let Mutation (items: IItemService) =
    // TODO: Add a way to add item descriptions for tooltips
    endpoints [
        endpoint "CreateItem" __ [
            description "Adds a new item that can be submitted"
            argumentDocumentation [
                "Name" => "The name of the item"
                "Description" => "The description for the item"
            ]

            validate (
                fun (args: {|Name: string; Description: string|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> task { return! items.Create args.Name args.Description })
        ]
        endpoint "ModifyItem" __ [
            description "Modifies an existing item that can be submitted"
            argumentDocumentation [
                "Id" => "The id of the item"
                "Name" => "The new name of the item"
                "Description" => "The new description for the item"
            ]

            validate (
                fun (args: {|Id: Guid; Name: string option; Description: string option|}) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> task { return! items.Edit args.Id args.Name args.Description })
        ]
    ]

let Types: Types = [
    ItemTypeGraph
]
