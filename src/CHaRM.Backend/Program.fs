module CHaRM.Backend.Program

open System
open System.IdentityModel.Tokens.Jwt
open System.Text
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open GraphQL.FSharp.Server
open GraphQL.Types
open GraphQL.Server
open GraphQL.Server.Ui.Playground
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.AspNetCore.Identity.UI.Services
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.EntityFrameworkCore
open Microsoft.IdentityModel.Tokens

open CHaRM.Backend.Model
open CHaRM.Backend.Provider
open CHaRM.Backend.Util

type ApplicationDbContext (context: DbContextOptions<ApplicationDbContext>) =
    inherit IdentityDbContext<User> (context)

let ensureDbCreated (Inject (config: IConfigurationRoot)) =
    let dbConfig =
        DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(config.GetConnectionString "Database")
            .Options
    use context = new ApplicationDbContext (dbConfig)
    context.Database.EnsureCreated () |> ignore

let getConfig () =
    ConfigurationBuilder()
        .SetBasePath(@"C:\Users\nimas\Repositories\CHaRM\Backend\src\CHaRM.Backend")
        .AddJsonFile("appsettings.json")
        .Build()

let addRoles (Inject (roleManager: RoleManager<IdentityRole>)) =
    [|"Visitor"; "Employee"; "Administrator"|]
    |> Array.iter (fun role ->
        let task =
            task {
                let! exists = roleManager.RoleExistsAsync role
                if not exists then
                    do!
                        IdentityRole role
                        |> roleManager.CreateAsync
                        :> Task
            }
            :> Task
        task.Wait ()
    )

let configure (app: IApplicationBuilder) =
    app.UseAuthentication()
    |> ignore

    app.UseCors(fun policy ->
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
        |> ignore)
    |> ignore
    app.UseWebSockets() |> ignore
    app.UseGraphQL<Schema> "/graphql" |> ignore
    app.UseGraphQLWebSockets<Schema> "/graphql" |> ignore
    app.UseGraphQLPlayground (GraphQLPlaygroundOptions ()) |> ignore

    ensureDbCreated app.ApplicationServices
    addRoles app.ApplicationServices
    ()

let configureServices (services: IServiceCollection) =
    let config = getConfig ()

    services.AddSingleton<IConfigurationRoot> config
    |> ignore

    services
        .AddDbContext<ApplicationDbContext>(
            fun options ->
                config.GetConnectionString "Database"
                |> options.UseSqlServer
                |> ignore
        )
    |> ignore

    services
        .AddDefaultIdentity<User>(
            fun options ->
                options.User.RequireUniqueEmail <- true
        )
        .AddRoles<IdentityRole>()
        .AddRoleManager<RoleManager<IdentityRole>>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
    |> ignore

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear ()
    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(
            fun options ->
                options.TokenValidationParameters <-
                    TokenValidationParameters (
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config.["Security:Tokens:Issuer"],
                        ValidAudience = config.["Security:Tokens:Audience"],
                        IssuerSigningKey = SymmetricSecurityKey (Encoding.UTF8.GetBytes config.["Security:Tokens:Key"])
                    )
        )
    |> ignore

    services.AddSingleton<IEmailSender> (
        implementationFactory = fun _ ->
        {
            new IEmailSender with
                member __.SendEmailAsync (email, subject, htmlMessage) =
                    failwith "Not Implemented"
        }
    )
    |> ignore

    services.AddTransient<UserProvider> (
        implementationFactory = fun services ->
            UserProvider.Create services
    )
    |> ignore


    services.AddCors () |> ignore
    services
        .AddSingleton<Schema>(
            implementationFactory =
                fun provider ->
                    Schema.Schema provider
        )
        .AddGraphQL(fun options ->
            options.ExposeExceptions <- true
            options.EnableMetrics <- true
        )
        .AddWebSockets()
        .AddDefaultFieldNameConverter()
        .AddGraphQLAuthorization(
            fun options ->
                // TODO: Document JwtPolicy
                options.AddJwtPolicy
                    "Authenticated"
                    (
                        fun policy ->
                            policy.RequireAuthenticatedUser()
                    )

                options.AddJwtPolicy
                    "Visitor"
                    (
                        fun policy ->
                            policy
                                .RequireAuthenticatedUser()
                                .RequireRole "Visitor"
                    )
        )
        |> ignore

    ()

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configure)
        .ConfigureServices(configureServices)
        .Build()
        .Run()

    0
