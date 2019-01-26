module CHaRM.Backend.Program

(* This module contains the boilerplate code for setting up the actual web server and providing the API. *)

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open GraphQL.Types
open GraphQL.Server
open GraphQL.Server.Ui.Playground

let configureApp (app: IApplicationBuilder) =
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

let configureServices (services: IServiceCollection) =
    services.AddCors () |> ignore
    services
        .AddSingleton<Schema>(Schema.Schema)
        .AddGraphQL(fun options ->
            options.ExposeExceptions <- true
            options.EnableMetrics <- true)
        .AddWebSockets()
        |> ignore
    ()

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()

    0
