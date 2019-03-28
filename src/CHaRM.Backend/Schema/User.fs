module CHaRM.Backend.Schema.User

open FSharp.Utils.Tasks
open GraphQL.FSharp
open GraphQL.FSharp.Builder
open GraphQL.FSharp.Server
open Validation.Builder

open CHaRM.Backend.Model
open CHaRM.Backend.Services

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
                resolve.property (fun this -> task { return this.ZipCode })
            ]
        ]
    ]

let Query (users: IUserService) =
    endpoints [
        endpoint "MyUser" __ [
            description "The current user"

            authorize LoggedIn
            resolve.endpoint (fun _ -> task { return! users.Me () })
        ]
    ]

let Mutation (users: IUserService) =
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

let Types: Types = [
    UserTypeGraph
    UserGraph
]
