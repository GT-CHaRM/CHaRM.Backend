module CHaRM.Program

open System
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open GraphQL
open GraphQL.Authorization
open GraphQL.Server
open GraphQL.Server.Ui.GraphiQL
open GraphQL.Server.Ui.Playground
open GraphQL.Server.Transports.Subscriptions.Abstractions
open GraphQL.Validation

open Util.Validation

[<AutoOpen>]
module Authorization =
    type IGraphQLBuilder with
        member this.AddGraphQLAuthorization () =
            this.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>() |> ignore
            this.Services.AddTransient<IValidationRule, AuthorizationValidationRule>().AddAuthorization() |> ignore
            this

        member this.AddGraphQLAuthorization (configure: AuthorizationOptions -> unit) =
            this.Services
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>() |> ignore
            this.Services
                .AddTransient<IValidationRule, AuthorizationValidationRule>()
                .AddAuthorization(Action<AuthorizationOptions> configure) |> ignore
            this


let configureApp (app: IApplicationBuilder) =
    ignore <| app.UseCors(fun builder ->
        builder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
            .AllowCredentials()
            |> ignore)
    ignore <| app.UseWebSockets ()
    ignore <| app.UseGraphQLWebSockets<Schema.Schema> "/graphql"
    ignore <| app.UseGraphQL<Schema.Schema> "/graphql"
    ignore <| app.UseGraphiQLServer (GraphiQLOptions())
    ignore <| app.UseGraphQLPlayground (GraphQLPlaygroundOptions())

let configureServices (services: IServiceCollection) =
    ignore <| services.AddCors()
    services.AddSingleton<IDependencyResolver>(
        implementationFactory = Func<IServiceProvider, IDependencyResolver> (
            fun provider ->
                FuncDependencyResolver(fun t -> provider.GetRequiredService t) :> IDependencyResolver))
    |> ignore

    ignore <| services
        .AddTransient<IValidationRule, Schema.Validation.GenericValidationType>()

    ignore <| services
        .AddSingleton<Schema.Schema>()
        .AddSingleton<Schema.UserType>()
        .AddSingleton<Schema.UserCategoryType>()
        .AddSingleton<Schema.ItemTypeType>()
        .AddSingleton<Schema.ItemInputType>()
        .AddSingleton<Schema.SubmissionType>()
        .AddSingleton<Schema.VisitorType>()
        .AddSingleton<Schema.EmployeeType>()
        .AddSingleton<Schema.AdministratorType>()
        .AddSingleton<Schema.Query>()
    ignore <| Mock.register services
    ignore <| services.AddGraphQL(fun options ->
        options.ExposeExceptions <- true
        options.EnableMetrics <- true)
        .AddUserContextBuilder(fun _ -> UserContext())
        .AddWebSockets()
        .AddDataLoader()
        // TODO
        // .AddGraphQLAuthorization(fun options ->
        //     ())

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0
