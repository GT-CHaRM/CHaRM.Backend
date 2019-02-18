[<AutoOpen>]
module CHaRM.Backend.Provider.User

open System
open System.Security.Claims
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.Configuration

open CHaRM.Backend.Model
open CHaRM.Backend.Jwt
open CHaRM.Backend.Util

let internal (|Guid|_|) (id: string) =
    match Guid.TryParse id with
    | true, id -> Some id
    | _ -> None

let internal getMyId (contextAccessor: IHttpContextAccessor) =
    contextAccessor.HttpContext.User
    |> Option.ofObj
    |> Option.bind (fun user -> Option.ofObj (user.FindFirst ClaimTypes.NameIdentifier))
    |> Option.map (fun claim -> claim.Value)
    |> Option.bind (|Guid|_|)

let all
    (users: UserManager<User>) =
    users.Users
    |> Seq.toArray
    |> Task.FromResult

let get
    (users: UserManager<User>)
    (id: Guid) =
    task {
        let! user = users.FindByIdAsync (id.ToString())
        return Option.ofBox user
    }

let me
    (context: IHttpContextAccessor)
    (users: UserManager<User>) =
    task {
        match getMyId context with
        | Some id -> return! get users id
        | _ -> return None
    }

let login
    (config: IConfigurationRoot)
    (signIn: SignInManager<User>)
    (users: UserManager<User>)
    username password =
    task {
        let! result =
            signIn.PasswordSignInAsync (
                userName = username,
                password = password,
                isPersistent = true,
                lockoutOnFailure = true
            )
        match result with
        | SignInSuccess ->
            let! user = users.FindByNameAsync username
            let! token = generateJwtToken config users user
            return Ok token
        | SignInError error -> return Error error
    }

let register
    (config: IConfigurationRoot)
    (users: UserManager<User>)
    username password email =
    task {
        let user =
            User (
                UserName = username,
                Email = email
            )
        let! result =
            users.CreateAsync (
                user = user,
                password = password
            )
        let! roleResult = users.AddToRoleAsync (user, "Visitor")
        match result, roleResult with
        | IdentitySuccess, IdentitySuccess ->
            let! token = generateJwtToken config users user
            return Ok token
        | IdentityError error, _
        | _, IdentityError error -> return Error error
    }

type UserProvider =
    {
        All: unit -> User [] Task
        Get: Guid -> User option Task
        Me: unit -> User option Task
        Login: string -> string -> Result<string, string> Task
        Register: string -> string -> string -> Result<string, string> Task
    }

    static member Create (Inject (context, config, users, signIn)) =
        {
            All = fun () -> all users
            Get = get users
            Me = fun () -> me context users
            Login = login config signIn users
            Register = register config users
        }
