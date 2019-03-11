[<AutoOpen>]
module CHaRM.Backend.Schema.Schema

open GraphQL.FSharp

let Schema (items, submissions, users) =
    schema {
        query [
            Query.Item items
            Query.Submission submissions
            Query.User users
        ]
        mutation [
            Mutation.Item items
            Mutation.Submission submissions
            Mutation.User users
        ]
        types [
            ItemTypeGraph
            ItemSubmissionBatchGraph
            SubmissionGraph
            UserGraph
        ]
    }
