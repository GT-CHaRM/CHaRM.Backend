module CHaRM.Backend.Schema

(* This module contains the Schema definition. The Schema  *)

open System
open GraphQL.FSharp
open GraphQL.FSharp.Builder

open CHaRM.Backend.Model
open CHaRM.Backend.Provider

(* Automatically converting our model to be Schema *)
let ItemType = Auto.Object<ItemType>
let ItemSubmissionBatch = Auto.Object<ItemSubmissionBatch>
let Submission = Auto.Object<Submission>
let IUser = Auto.Interface<IUser>
let Visitor = Auto.Object<Visitor>
let Employee = Auto.Object<Employee>
let Administrator = Auto.Object<Administrator>

/// Queries are requests to the server that *get* some data without making any changes to it.
/// For example, getting a list of all of my submissions is a query.
let Query =
    query {
        /// Every **field** is a specific request.
        /// The function inside the `resolve` or `resolveAsync` method is called whenever that request is made.
        /// The return value of that function is returned to the client.
        fields [
            field {
                name "items"
                resolveAsync (fun _ -> itemProvider.All ())
            }

            field {
                name "item"
                arguments [
                    Define.Argument<Guid> "id"
                ]
                resolveAsync (fun ctx ->
                    ctx.GetArgument<Guid> "id"
                    |> itemProvider.Get)
            }

            field {
                name "submissions"
                resolveAsync (fun _ ->
                    submissionProvider.All ())
            }


            field {
                name "submission"
                arguments [
                    Define.Argument<Guid> "id"
                ]
                resolveAsync (fun ctx ->
                    ctx.GetArgument<Guid> "id"
                    |> submissionProvider.Get)
            }
        ]
    }

/// Mutations are requests made by the client that *change* something on the server's database.
/// For example, adding a new submission is a mutation.
let Mutation =
    mutation {
        fields [
            field {
                name "createItem"
                arguments [
                    Define.Argument<string> "name"
                ]
                resolveAsync (fun ctx ->
                    ctx.GetArgument<string> "name"
                    |> itemProvider.Create)
            }

            field {
                name "createSubmission"
                arguments [
                    Define.Argument<Guid System.Collections.Generic.List> "items"
                ]
                resolveAsync (fun ctx ->
                    ctx.GetArgument<Guid System.Collections.Generic.List> "items"
                    |> Seq.toList
                    |> submissionProvider.Create)
            }
        ]
    }

let Schema =
    schema {
        query Query
        mutation Mutation
    }
