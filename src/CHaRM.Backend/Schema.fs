module CHaRM.Backend.Schema

(*
    This module contains the GraphQL Schema definition.
    The GraphQL Schema provides us with a way of defining all of our data types in a language-independent manner.
    For example, a user type might be the following in Java:
    ```
        public class User {
            private String name;
            private int age;

            public Bicycle(String name, int age) {
                this.name = name;
                this.age = age;
            }

            public String getName() { return name; }
            public String setName(String name) { this.name = name; }

            public String getAge() { return age; }
            public String setAge(String age) { this.age = age; }
        }
    ```
    but that same user might be typed as such in TypeScript:
    ```
        interface User {
            name: string;
            age: number;
        }
    ```
    and in JavaScript, there's no static typing so the object is just plain JSON!

    GraphQL gives us a langauge to define our data in 1 language and have it work predictably across all of the languages we use that data in.
    The point of the Schema module is to convert our F# data types into GraphQL schema types.
*)

open System
open GraphQL.FSharp
open GraphQL.FSharp.Builder

open CHaRM.Backend.Model
open CHaRM.Backend.Provider

(* Automatically converting our model to Schema *)
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
