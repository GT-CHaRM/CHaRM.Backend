module CHaRM.Backend.Provider

(* This module contains the interfaces (and mock implementations) of our methods that access/modify the database. *)

open System
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Identity

open CHaRM.Backend.Model

type JwtToken = JwtToken of string

type LoginInput = {
    Username: string
    Password: string
}

type RegisterInput = {
    Username: string
    Email: string
    Password: string
}

type LoginError =
| LockedOut
| NotAllowed

type RegisterError =
| Errors of IdentityError seq

let (|Success|Error|) (result: SignInResult) =
    if result.Succeeded then Success
    elif result.IsLockedOut then Error LockedOut
    else Error NotAllowed

let (|RegisterSuccess|RegisterError|) (result: IdentityResult) =
    if result.Succeeded then RegisterSuccess
    else RegisterError (Errors result.Errors)

type UserProvider = {
    All: unit -> ApplicationUser list Task
    Login: LoginInput -> Result<JwtToken, LoginError> Task
    Register: RegisterInput -> Result<JwtToken, RegisterError> Task
}

let generateJwtToken (user: ApplicationUser) =
    JwtToken ""

let makeUserProvider (signIn: SignInManager<ApplicationUser>) (users: UserManager<ApplicationUser>) =
    let all () =
        users.Users
        |> Seq.toList
        |> Task.FromResult

    let login ({Username = username; Password = password}: LoginInput) =
        task {
            match!
                signIn.PasswordSignInAsync (
                    userName = username,
                    password = password,
                    isPersistent = true,
                    lockoutOnFailure = true
                ) with
            | Success ->
                let! user = users.FindByNameAsync username
                return Ok (generateJwtToken user)
            | Error e -> return Error e
        }

    let register {Username = username; Email = email; Password = password} =
        task {
            let user =
                ApplicationUser (
                    UserName = username,
                    Email = email
                )
            match!
                users.CreateAsync (
                    user = user,
                    password = password
                ) with
            | RegisterSuccess -> return Ok (generateJwtToken user)
            | RegisterError error -> return Error (Errors error)
        }

    {
        All = all
        Login = login
        Register = register
    }

/// The methods that access/modify the list of submittable items in our database.
type ItemProvider = {
    All: unit -> ItemType list Task
    Create: string -> ItemType Task
    Get: Guid -> ItemType Task
}

/// The methods that access/modify the list of submissions by visitors in our database.
type SubmissionProvider = {
    All: unit -> Submission list Task
    Create: Guid list -> Submission Task
    Get: Guid -> Submission Task
}

(* Mock implementations *)

let mutable items = [{Id = Guid.NewGuid (); Name = "myName"}]
let itemProvider: ItemProvider = {
    All = fun () -> Task.FromResult items
    Create = fun name ->
        let item = {
            Id = Guid.NewGuid ()
            Name = name
        }
        items <- item :: items
        Task.FromResult item
    Get = fun id ->
        items
        |> List.find (fun {Id = id'} -> id = id')
        |> Task.FromResult
}

let mutable submissions = [{Id = Guid.NewGuid (); Items = [{Item = items.[0]; Count = 5}]; Submitted = DateTimeOffset.Now}]
let submissionProvider: SubmissionProvider = {
    All = fun () -> Task.FromResult submissions
    Create = fun submittedItems ->
        let items =
            submittedItems
            |> List.groupBy id
            |> List.map (fun (id, lst) ->
                {
                    Item = items |> List.find (fun item -> item.Id = id)
                    Count = List.length lst
                })
        let item = {
            Id = Guid.NewGuid ()
            Submitted = DateTimeOffset.Now
            Items = items
        }
        submissions <- item :: submissions
        Task.FromResult item
    Get = fun id ->
        submissions
        |> List.find (fun {Id = id'} -> id = id')
        |> Task.FromResult
}
