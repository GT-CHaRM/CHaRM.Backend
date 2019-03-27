[<AutoOpen>]
module CHaRM.Backend.Schema.Utils

open System.Security.Claims
open GraphQL.FSharp
open GraphQL.FSharp.Server
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authorization

open CHaRM.Backend.Model

type AuthorizationPolicyBuilder with
    member this.RequireUserType (``type``: UserType) =
        this
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireClaim(ClaimTypes.Role, string ``type``)

type Policy =
    | LoggedIn
    | Visitor
    | Employee
    | Administrator

    interface IPolicy with
        member this.Authorize builder =
            match this with
            | LoggedIn ->
                builder
                    .RequireAuthenticatedUser()
            | Visitor ->
                builder
                    .RequireAuthenticatedUser()
                    .RequireUserType(UserType.Visitor)
            | Employee ->
                builder
                    .RequireAuthenticatedUser()
                    .RequireUserType(UserType.Employee)
            | Administrator ->
                builder
                    .RequireAuthenticatedUser()
                    .RequireUserType(UserType.Administrator)
