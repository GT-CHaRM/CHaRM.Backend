module CHaRM.Backend.Util

open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Identity

let (|Default|_|) obj =
    if obj = Unchecked.defaultof<_>
    then Some ()
    else None

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
