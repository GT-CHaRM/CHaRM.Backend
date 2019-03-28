[<AutoOpen>]
module CHaRM.Backend.Schema.Schema

open GraphQL.FSharp.Builder

let Query items submissions users =
    query [
        yield! Item.Query items
        yield! Submission.Query submissions
        yield! User.Query users
    ]

let Mutation items submissions users =
    mutation [
        yield! Item.Mutation items
        yield! Submission.Mutation users submissions
        yield! User.Mutation users
    ]

let Schema (items, submissions, users) =
    schema [
        Query items submissions users
        Mutation items submissions users

        types [
            yield! Item.Types
            yield! Submission.Types
            yield! User.Types
        ]
    ]
