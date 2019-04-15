module CHaRM.Backend.Schema.User

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
        type User = {Id: Guid}
    module Mutation =
        type LoginUser = {Username: string; Password: string}
        type RegisterUser = {Username: string; Email: string; Password: string; ZipCode: string}
        type ChangeUserPassword = {Id: Guid; NewPassword: string}
        type ChangeMyPassword = {OldPassword: string; NewPassword: string}
        type DeleteMyAccount = {Password: string}
        type DeleteAccount = {Id: Guid}
        type ChangeZipCode = {Id: Guid; ZipCode: string}
        type ChangeMyZipCode = {ZipCode: string}
        type CreateEmployeeAccount = {Username: string; Email: string; Password: string}

let UserTypeGraph =
    enum<UserType> [
        Documentation.description "A specific type of user"
        Enum.cases [
            Enum.case UserType.Visitor []
            Enum.case UserType.Employee []
            Enum.case UserType.Administrator []
        ]
    ]

let UserGraph =
    object<User> [
        Documentation.description "A user registered with CHaRM"
        fields [
            field __ [
                Documentation.description "The ID of the user"
                resolve.property (fun this -> vtask { return this.Id })
            ]

            field __ [
                Documentation.description "The type of the user"
                resolve.property (fun this -> vtask { return this.Type })
            ]

            field __ [
                Documentation.description "The user's unique username"
                resolve.property (fun this -> vtask { return this.UserName })
            ]

            field __ [
                Documentation.description "The user's email"
                resolve.property (fun this -> vtask { return this.Email })
            ]

            field __ [
                Documentation.description "The user's zip code"
                resolve.property (fun this -> vtask { return this.ZipCode })
            ]
        ]
    ]

let Query (users: IUserService) =
    endpoints [
        endpoint "MyUser" __ [
            Documentation.description "The current user"

            authorize LoggedIn
            resolve.endpoint (fun _ -> vtask { return! users.Me () })
        ]
        endpoint "User" __ [
            Documentation.description "A single user"
            validate (
                fun (args: Query.User) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! users.Get args.Id })
        ]
        // TODO: Fix type inference
        endpoint "AllUsers" (NullGraph <| ListGraph UserGraph) [
            Documentation.description "List of all users"

            // authorize
            resolve.endpoint (fun _ -> vtask { return! users.All () })
        ]
     ]

let Mutation (users: IUserService) =
    endpoints [
        endpoint "LoginUser" __ [
            Documentation.description "Attempts to login with the provided username and password and returns a JSON web token (JWT) on success."
            Documentation.arguments [
                "Username" => "The user's uesrname"
                "Password" => "The user's password"
            ]

            validate (
                fun (args: Mutation.LoginUser) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! users.Login args.Username args.Password })
        ]

        // TODO: Fix issue with getting failure after the first mistake
        endpoint "RegisterUser" __ [
            Documentation.description "Attempts to register with the provided information and returns a JSON web token (JWT) on success."
            Documentation.arguments [
                "Username" => "The user's username"
                "Email" => "The user's email"
                "Password" => "The user's password"
                "ZipCode" => "The user's zip code"
            ]

            validate (
                fun (args: Mutation.RegisterUser) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! users.Register args.Username args.Password args.Email args.ZipCode })
        ]

        endpoint "ChangeUserPassword" __ [
            Documentation.description "Changes the zip code of the current user"
            Documentation.arguments [
                "Id" => "The ID of the user"
                "NewPassword" => "The new password"
            ]

            validate (
                fun (args: Mutation.ChangeUserPassword) -> validation {
                    return args
                }
            )
            resolve.endpoint (
                fun args ->
                    vtask {
                        let! me = % users.Me ()
                        return! users.ForceChangePassword me.Id args.NewPassword
                    }
                )
        ]

        endpoint "ChangeMyPassword" __ [
            Documentation.description "Changes the zip code of the current user"
            Documentation.arguments [
                "OldPassword" => "The old password"
                "NewPassword" => "The new password"
            ]

            validate (
                fun (args: Mutation.ChangeMyPassword) -> validation {
                    return args
                }
            )
            resolve.endpoint (
                fun args ->
                    vtask {
                        let! me = % users.Me ()
                        return! users.ChangePassword me.Id args.OldPassword args.NewPassword
                    }
                )
        ]

        endpoint "DeleteMyAccount" __ [
            Documentation.description "Deletes the current account"
            Documentation.arguments [
                "Password" => "The current password of the user"
            ]

            validate (
                fun (args: Mutation.DeleteMyAccount) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! users.DeleteMyAccount args.Password })
        ]

        endpoint "DeleteAccount" __ [
            // authorize Employee
            Documentation.description "Deletes the current account"
            Documentation.arguments [
                "Id" => "The id of the user to remove"
            ]

            validate (
                fun (args: Mutation.DeleteAccount) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! users.DeleteAccount args.Id })
        ]

        endpoint "ChangeMyZipCode" __ [
            Documentation.description "Changes the zip code of the current user"
            Documentation.arguments [
                "ZipCode" => "The new zip code"
            ]

            validate (
                fun (args: Mutation.ChangeMyZipCode) -> validation {
                    return args
                }
            )
            resolve.endpoint (
                fun args ->
                    vtask {
                        let! me = % users.Me ()
                        return! users.ChangeZipCode me.Id args.ZipCode
                    }
                )
        ]

        endpoint "ChangeUserZipCode" __ [
            Documentation.description "Changes the zip code of the current user"
            Documentation.arguments [
                "Id" => "The user whose zip code is being changed"
                "ZipCode" => "The new zip code"
            ]

            validate (
                fun (args: Mutation.ChangeZipCode) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! users.ChangeZipCode args.Id args.ZipCode })
        ]

        endpoint "CreateEmployeeAccount" __ [
            // authorize Administrator
            Documentation.description "Creates an employee account"
            Documentation.arguments [
                "Username" => "The employee's username"
                "Email" => "The employee's email"
                "Password" => "The employee's password"
            ]

            validate (
                fun (args: Mutation.CreateEmployeeAccount) -> validation {
                    return args
                }
            )
            resolve.endpoint (fun args -> vtask { return! users.RegisterEmployee args.Username args.Password args.Email })
        ]
    ]

let Types: Types = [
    UserTypeGraph
    UserGraph
]
