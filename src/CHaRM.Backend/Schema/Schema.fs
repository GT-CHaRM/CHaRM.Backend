[<AutoOpen>]
module CHaRM.Backend.Schema.Schema

open GraphQL.FSharp.Builder

let Query items submissions users =
    query [
        yield! Query.Item items
        yield! Query.Submission submissions
        yield! Query.User users
    ]

let Mutation items submissions users =
    mutation [
        yield! Mutation.Item items
        yield! Mutation.Submission users submissions
        yield! Mutation.User users
    ]

let Schema (items, submissions, users) =
    schema [
        Query items submissions users
        Mutation items submissions users

        types [
            ItemTypeGraph
            ItemSubmissionBatchGraph
            SubmissionGraph
            UserGraph
        ]
    ]
