module CHaRM.Backend.Program

open System
open System.IdentityModel.Tokens.Jwt
open System.Text
open FSharp.Utils
open FSharp.Utils.Tasks
open GraphQL.FSharp.Server
open GraphQL.Server
open GraphQL.Server.Ui.Playground
open GraphQL.Types
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

open CHaRM.Backend.Database
open CHaRM.Backend.Model
open CHaRM.Backend.Schema
open CHaRM.Backend.Services


let ensureDbCreated (app: IApplicationBuilder) =
    use serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope()
    let context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext> ()
    // context.Database.EnsureDeleted () |> ignore
    context.Database.EnsureCreated () |> ignore

let getConfig () =
    ConfigurationBuilder()
        // .SetBasePath(@"C:\Users\nimas\Repositories\CHaRM\Backend\src\CHaRM.Backend")
        .AddJsonFile("appsettings.json")
        .Build()

let (|Default|_|) value = if value = Unchecked.defaultof<_> then Some () else None

let addAdminAccount (users: UserManager<User>) =
    // TODO: Remove hardcoded values
    unitTask {
        match! users.FindByNameAsync "admin" with
        | Default ->
            let! _ =
                users.CreateAsync (
                    user =
                        User (
                            Type = UserType.Administrator,
                            UserName = "admin",
                            Email = "admin@livethrive.org",
                            ZipCode = "",
                            SecurityStamp = Guid.NewGuid().ToString()
                        ),
                    password = "MyPass1$"
                )
            ()
        | _ -> ()
    }

let addDefaultItems (items: IItemService) =
    unitTask {
        let! all = items.All ()
        if not <| Array.isEmpty all then return () else
        let! _ =
            items.Create
                "Paint"
                "latex and oil base (First fifty pounds are free each additional pound is $.25.)"
        let! _ =
            items.Create
                "Household Chemicals"
                "pesticides, herbicides, household cleaners, etc. (The first 50 pounds are free each additional pound is $.25)"
        return ()
    }

let configure (app: IApplicationBuilder) =
    app.UseWebSockets ()
    |> ignore

    app.UseAuthentication ()
    |> ignore

    app.UseCors(
        fun policy ->
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
            |> ignore
    )
    |> ignore

    app.UseGraphQL<Schema> "/graphql" |> ignore
    app.UseGraphQLWebSockets<Schema> "/graphql" |> ignore
    app.UseGraphQLPlayground (GraphQLPlaygroundOptions ()) |> ignore

    ensureDbCreated app

    addAdminAccount(app.ApplicationServices.GetRequiredService<UserManager<User>> ()).Wait()
    addDefaultItems(app.ApplicationServices.GetRequiredService<IItemService> ()).Wait()

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

    services.AddHttpContextAccessor ()
    |> ignore

    services.AddTransient<IItemService, ItemService> ()
    |> ignore

    services.AddTransient<ISubmissionService, SubmissionService> ()
    |> ignore

    services.AddTransient<IUserService, UserService> ()
    |> ignore

    services.AddCors () |> ignore
    services
        .AddSingleton<Schema>(
            implementationFactory = Func<_, _> (DependencyInjection.resolve >> Schema)
        )
        .AddGraphQL(fun options ->
            options.ExposeExceptions <- true
            options.EnableMetrics <- true
        )
        .AddDefaultFieldNameConverter()
        .AddAuthorization<Policy> id
    |> ignore

    ()

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseUrls("http://0.0.0.0:5000")
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configure)
        .ConfigureServices(configureServices)
        .Build()
        .Run()

    0
