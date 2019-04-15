[<AutoOpen>]
module CHaRM.Backend.Services.User

open System
open System.Security.Claims
open System.Threading.Tasks
open FSharp.Utils
open FSharp.Utils.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration

open CHaRM.Backend.Error
open CHaRM.Backend.Model
open CHaRM.Backend.Jwt
open CHaRM.Backend.Util

let inline (~%%) (error: IdentityResult Task) =
    ValueTaskResult <| vtask {
        let! error = error
        if error.Succeeded then return Ok () else
        return
            error.Errors
            |> Seq.toList
            |> List.map (fun error -> IdentityError error.Description)
            |> Error
    }

let internal (|Guid|_|) (id: string) =
    match Guid.TryParse id with
    | true, id -> Some id
    | _ -> None

let internal getMyId (http: HttpContext) =
    http.User.FindFirst ClaimTypes.NameIdentifier
    |> Option.ofObj
    |> Option.bind (fun claim -> Option.ofObj claim.Value)
    |> Option.bind (|Guid|_|)

let get
    (users: UserManager<User>)
    (id: Guid) =
    vtask {
        let! user = users.FindByIdAsync (id.ToString ())
        match Option.ofNullObj user with
        | Some user -> return Ok user
        | _ -> return Error [NoUserFound id]
    }

let me
    (contextAccessor: IHttpContextAccessor)
    (users: UserManager<User>) =
    vtask {
        match getMyId contextAccessor.HttpContext with
        | Some id -> return! get users id
        | _ -> return Error [NotLoggedIn]
    }

module Query =
    let toArrayAsync (queriable: _ Linq.IQueryable) = queriable.ToArrayAsync ()

let all contextAcessor (users: UserManager<User>) =
    vtask {
        let! me = % me contextAcessor users
        let! users =
            query {
                for user in users.Users do
                    where (int user.Type < int me.Type)
                    select user
            }
            |> Query.toArrayAsync
        return Ok users
    }

let login
    (config: IConfigurationRoot)
    (signIn: SignInManager<User>)
    (users: UserManager<User>)
    username password =
    vtask {
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
    vtask {
        let user =
            User (
                Type = type',
                UserName = username,
                Email = email,
                ZipCode = zip,
                SecurityStamp = Guid.NewGuid().ToString()
            )
        do!
            %% users.CreateAsync (
                user = user,
                password = password
            )
        let! token = generateJwtToken config user
        return Ok token
    }

let changePassword (users: UserManager<User>) id old ``new`` =
    vtask {
        let! user = % get users id
        match! users.ChangePasswordAsync (user, old, ``new``) with
        | IdentitySuccess -> return Ok user
        | IdentityError error -> return Error [IdentityError error]
    }

let forceChangePassword (users: UserManager<User>) id ``new`` =
    vtask {
        let! user = % get users id
        let! token = users.GeneratePasswordResetTokenAsync user
        match! users.ResetPasswordAsync (user, token, ``new``) with
        | IdentitySuccess -> return Ok user
        | IdentityError error -> return Error [IdentityError error]
    }

let changeZipCode (users: UserManager<User>) id zip =
    vtask {
        let! user = % get users id
        user.ZipCode <- zip
        match! users.UpdateAsync user with
        | IdentitySuccess -> return Ok user
        | IdentityError error -> return Error [IdentityError error]
    }

let deleteAccount users id =
    vtask {
        let! user = % get users id
        do! %% users.DeleteAsync user
        return Ok user
    }

let deleteMyAccount contextAccessor users password =
    vtask {
        let! user = % me contextAccessor users
        match! users.CheckPasswordAsync (user, password) with
        | true ->
            do! %% users.DeleteAsync user
            return Ok user
        | false -> return Error [InvalidPassword]
    }

type IUserService =
    abstract member All: unit -> Result<User [], ErrorCode list> ValueTask
    abstract member Get: id: Guid -> Result<User, ErrorCode list> ValueTask
    abstract member Me: unit -> Result<User, ErrorCode list> ValueTask
    abstract member Login: username: string -> password: string -> Result<string, ErrorCode list> ValueTask
    abstract member Register: username: string -> password: string -> email: string -> zip: string -> Result<string, ErrorCode list> ValueTask
    abstract member RegisterEmployee: username: string -> password: string -> email: string -> Result<string, ErrorCode list> ValueTask
    abstract member ChangePassword: id: Guid -> old: string -> ``new``: string -> Result<User, ErrorCode list> ValueTask
    abstract member ForceChangePassword: id: Guid -> ``new``: string -> Result<User, ErrorCode list> ValueTask
    abstract member ChangeZipCode: id: Guid -> zip: string -> Result<User, ErrorCode list> ValueTask
    abstract member DeleteAccount: id: Guid -> Result<User, ErrorCode list> ValueTask
    abstract member DeleteMyAccount: password: string -> Result<User, ErrorCode list> ValueTask

type UserService (config, contextAccessor, users, signIn) =
    let all () = all contextAccessor users
    let get id = get users id
    let me () = me contextAccessor users
    let login username password = login config signIn users username password
    let register type' username password email zip = register config users type' username password email zip
    let changePassword id old ``new`` = changePassword users id old ``new``
    let forceChangePassword id ``new`` = forceChangePassword users id ``new``
    let changeZipCode id zip = changeZipCode users id zip
    let deleteAccount id = deleteAccount users id
    let deleteMyAccount password = deleteMyAccount contextAccessor users password

    interface IUserService with
        member __.All () = all ()
        member __.Get id = get id
        member __.Me () = me ()
        member __.Login username password = login username password
        member __.Register username password email zip = register UserType.Visitor username password email zip
        member __.RegisterEmployee username password email = register UserType.Employee username password email ""
        member __.ChangePassword id old ``new`` = changePassword id old ``new``
        member __.ForceChangePassword id ``new`` = forceChangePassword id ``new``
        member __.ChangeZipCode id zip = changeZipCode id zip
        member __.DeleteMyAccount password = deleteMyAccount password
        member __.DeleteAccount id = deleteAccount id
