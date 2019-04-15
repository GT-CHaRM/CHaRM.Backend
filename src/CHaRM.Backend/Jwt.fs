module CHaRM.Backend.Jwt

open System
open System.IdentityModel.Tokens.Jwt
open System.Threading.Tasks
open System.Security.Claims
open System.Text
open FSharp.Utils
open FSharp.Utils.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.IdentityModel.Tokens

open CHaRM.Backend.Model

// https://medium.com/@ozgurgul/asp-net-core-2-0-webapi-jwt-authentication-with-identity-mysql-3698eeba6ff8

let internal getClaims (user: #User) =
    [
        Claim (
            ``type`` = JwtRegisteredClaimNames.Sub,
            value = user.NormalizedEmail
        )
        Claim (
            ``type`` = JwtRegisteredClaimNames.Jti,
            value = string Guid.Random
        )
        Claim (
            ``type`` = ClaimTypes.NameIdentifier,
            value = string user.Id
        )
        Claim (
            ``type`` = ClaimTypes.Role,
            value = string user.Type
        )
    ]

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
    (user: #User) =
    task {
        return
            JwtSecurityToken (
                issuer = config.["Security:Tokens:Issuer"],
                audience = config.["Security:Tokens:Audience"],
                claims = getClaims user,
                expires = getExpiry config,
                signingCredentials = getSigningCredentials config
            )
            |> makeToken
    }
