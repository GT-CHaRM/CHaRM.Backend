module CHaRM.Backend.Jwt

open System
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open System.Text
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.Configuration
open Microsoft.IdentityModel.Tokens

open CHaRM.Backend.Model

// https://medium.com/@ozgurgul/asp-net-core-2-0-webapi-jwt-authentication-with-identity-mysql-3698eeba6ff8

let internal getClaims
    (users: UserManager<User>)
    (user: #User) =
    task {
        let! userRoles = users.GetRolesAsync user
        return [
            yield Claim (
                ``type`` = JwtRegisteredClaimNames.Sub,
                value = user.NormalizedEmail
            )
            yield Claim (
                ``type`` = JwtRegisteredClaimNames.Jti,
                value = Guid.NewGuid().ToString()
            )
            yield Claim (
                ``type`` = ClaimTypes.NameIdentifier,
                value = user.Id
            )
            yield! [
                for role in userRoles do
                    yield Claim (
                        ``type`` = ClaimTypes.Role,
                        value = role
                    )
            ]
        ]
    }

let internal makeHmacSha256SigningCredentials key =
    SigningCredentials (
        key = key,
        algorithm = SecurityAlgorithms.HmacSha256
    )

let internal getSigningCredentials
    (config: IConfigurationRoot) =
        config.["Security:Tokens:Key"]
        |> Encoding.UTF8.GetBytes
        |> SymmetricSecurityKey
        |> makeHmacSha256SigningCredentials

let internal getExpiry
    (config: IConfigurationRoot) =
    Convert.ToDouble config.["Security:Tokens:ExpireDays"]
    |> DateTime.Now.AddDays
    |> Nullable

let internal makeToken token = JwtSecurityTokenHandler().WriteToken token

let generateJwtToken
    (config: IConfigurationRoot)
    (users: UserManager<User>)
    (user: #User) =
    task {
        let! claims = getClaims users user

        return
            JwtSecurityToken (
                issuer = config.["Security:Tokens:Issuer"],
                audience = config.["Security:Tokens:Audience"],
                claims = claims,
                expires = getExpiry config,
                signingCredentials = getSigningCredentials config
            )
            |> makeToken
    }
