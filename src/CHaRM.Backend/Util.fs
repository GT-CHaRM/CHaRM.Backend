module CHaRM.Backend.Util

open System
open FSharp.Reflection
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.DependencyInjection

module Option =
    let ofBox x =
        match box x with
        | null -> None
        | _ -> Some x

let rec internal isValidInjectionType ``type`` =
    if ``type`` = typeof<obj>
    then false
    else
        not <| FSharpType.IsTuple ``type``
        || FSharpType.GetTupleElements ``type``
            |> Array.forall isValidInjectionType

let (|Inject|) (provider: IServiceProvider): 't =
    assert isValidInjectionType typeof<'t>

    if not <| FSharpType.IsTuple typeof<'t> then provider.GetService<'t> () else
    let tupleElements =
        FSharpType.GetTupleElements typeof<'t>
        |> Array.map provider.GetService
    FSharpValue.MakeTuple (
        tupleElements = tupleElements,
        tupleType = typeof<'t>
    )
    :?> 't

let (|SignInSuccess|SignInError|) (result: SignInResult) =
    if result.Succeeded then SignInSuccess
    elif result.IsLockedOut then SignInError "Locked out!"
    elif result.IsNotAllowed then SignInError "Not allowed!"
    else SignInError "Could not find the provided username and password combination!"

let (|IdentitySuccess|IdentityError|) (result: IdentityResult) =
    if result.Succeeded then IdentitySuccess
    else IdentityError (Seq.head result.Errors).Description

type AuthorizationOptions with
    // https://stackoverflow.com/a/48659618
    member this.AddJwtPolicy name builder =
        this.AddPolicy (
            name = name,
            configurePolicy = fun policy ->
                policy.AuthenticationSchemes.Add JwtBearerDefaults.AuthenticationScheme |> ignore
                builder policy |> ignore
        )
