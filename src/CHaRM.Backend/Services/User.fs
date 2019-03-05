[<AutoOpen>]
module CHaRM.Backend.Services.User

open System
open System.Security.Claims
open System.Threading.Tasks
open FSharp.Utils
open FSharp.Utils.Tasks
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
        return Option.ofNullObj user
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
            let! token = generateJwtToken config user
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
                Email = email,
                SecurityStamp = Guid.NewGuid().ToString()
            )
        let! result =
            users.CreateAsync (
                user = user,
                password = password
            )
        match result with
        | IdentitySuccess ->
            let! token = generateJwtToken config user
            return Ok token
        | IdentityError error -> return Error error
    }

type IUserService =
    abstract member All: unit -> User [] Task
    abstract member Get: id: Guid -> User option Task
    abstract member Me: unit -> User option Task
    abstract member Login: username: string -> password: string -> Result<string, string> Task
    abstract member Register: username: string -> password: string -> email: string -> Result<string, string> Task

type UserService (context, config, users, signIn) =
    let all () = all users
    let get id = get users id
    let me () = me context users
    let login username password = login config signIn users username password
    let register username password email = register config users username password email

    interface IUserService with
        member __.All () = all ()
        member __.Get id = get id
        member __.Me () = me ()
        member __.Login username password = login username password
        member __.Register username password email = register username password email
