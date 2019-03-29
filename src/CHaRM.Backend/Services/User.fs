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

open CHaRM.Backend.Error
open CHaRM.Backend.Model
open CHaRM.Backend.Jwt
open CHaRM.Backend.Util

let internal (|Guid|_|) (id: string) =
    match Guid.TryParse id with
    | true, id -> Some id
    | _ -> None

let internal getMyId (http: HttpContext) =
    http.User.FindFirst ClaimTypes.NameIdentifier
    |> Option.ofObj
    |> Option.bind (fun claim -> Option.ofObj claim.Value)
    |> Option.bind (|Guid|_|)

let all
    (users: UserManager<User>) =
    users.Users
    |> Seq.toArray
    |> Ok
    |> Task.FromResult

let get
    (users: UserManager<User>)
    (id: Guid) =
    task {
        let! user = users.FindByIdAsync (id.ToString ())
        match Option.ofNullObj user with
        | Some user -> return Ok user
        | _ -> return Error [NoUserFound id]
    }

let me
    (contextAccessor: IHttpContextAccessor)
    (users: UserManager<User>) =
    task {
        match getMyId contextAccessor.HttpContext with
        | Some id -> return! get users id
        | _ -> return Error [NotLoggedIn]
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
        | SignInError error -> return Error [SignInError error]
    }

let register
    (config: IConfigurationRoot)
    (users: UserManager<User>)
    type' username password email zip =
    task {
        let user =
            User (
                Type = type',
                UserName = username,
                Email = email,
                ZipCode = zip,
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
        | IdentityError error -> return Error [IdentityError error]
    }

// TODO: Add old password support in the future
let changePassword (users: UserManager<User>) id ``new`` =
    task {
        let! user = % get users id
        let! token = users.GeneratePasswordResetTokenAsync user
        match! users.ResetPasswordAsync (user, token, ``new``) with
        | IdentitySuccess -> return Ok user
        | IdentityError error -> return Error [IdentityError error]
    }

let changeZipCode (users: UserManager<User>) id zip =
    task {
        let! user = % get users id
        user.ZipCode <- zip
        match! users.UpdateAsync user with
        | IdentitySuccess -> return Ok user
        | IdentityError error -> return Error [IdentityError error]
    }

type IUserService =
    abstract member All: unit -> Result<User [], ErrorCode list> Task
    abstract member Get: id: Guid -> Result<User, ErrorCode list> Task
    abstract member Me: unit -> Result<User, ErrorCode list> Task
    abstract member Login: username: string -> password: string -> Result<string, ErrorCode list> Task
    abstract member Register: username: string -> password: string -> email: string -> zip: string -> Result<string, ErrorCode list> Task
    abstract member RegisterEmployee: username: string -> password: string -> email: string -> Result<string, ErrorCode list> Task
    abstract member ChangePassword: id: Guid -> ``new``: string -> Result<User, ErrorCode list> Task
    abstract member ChangeZipCode: id: Guid -> zip: string -> Result<User, ErrorCode list> Task

type UserService (config, contextAccessor, users, signIn) =
    let all () = all users
    let get id = get users id
    let me () = me contextAccessor users
    let login username password = login config signIn users username password
    let register type' username password email zip = register config users type' username password email zip
    let changePassword id ``new`` = changePassword users id ``new``
    let changeZipCode id zip = changeZipCode users id zip

    interface IUserService with
        member __.All () = all ()
        member __.Get id = get id
        member __.Me () = me ()
        member __.Login username password = login username password
        member __.Register username password email zip = register UserType.Visitor username password email zip
        member __.RegisterEmployee username password email = register UserType.Employee username password email ""
        member __.ChangePassword id ``new`` = changePassword id ``new``
        member __.ChangeZipCode id zip = changeZipCode id zip
