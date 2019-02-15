module CHaRM.Backend.Authentication

open System
open System.Text
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.EntityFrameworkCore
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Identity.UI.Services

type ApplicationDbContext () =
    inherit IdentityDbContext<ApplicationUser> ()

let emailSender =
    {
        new IEmailSender with
            member this.SendEmailAsync (email: string, subject: string, htmlMessage: string): Task =
                failwith "Not Implemented"
    }

let configureServices (config: IConfigurationRoot) (services: IServiceCollection) =
    services
        .AddDbContext<ApplicationDbContext>(
            fun options ->
                config.GetConnectionString "Database"
                |> options.UseSqlServer
                |> ignore
        )
    |> ignore

    services
        .AddIdentityCore<ApplicationUser>(
            fun options ->
                options.User.RequireUniqueEmail <- true
        )
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
    |> ignore

    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(
            fun options ->
                options.TokenValidationParameters <-
                    TokenValidationParameters (
                        ValidateIssuer = true,
                        ValidIssuer = config.["Security:Tokens:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = config.["Security:Tokens:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            SymmetricSecurityKey (
                                Encoding.UTF8.GetBytes config.["Security:Tokens:Key"]
                            )
                    )
        )
    |> ignore

    services.AddSingleton<IEmailSender> emailSender
    |> ignore

let configure (app: IApplicationBuilder) =
    app.UseAuthentication()
    |> ignore
