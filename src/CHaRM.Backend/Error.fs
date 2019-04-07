module CHaRM.Backend.Error

open System

type ErrorCode =
| NotLoggedIn
| SubmissionDoesNotExist of Id: Guid
| NoUserFound of Id: Guid
| SignInError of Error: string
| IdentityError of Error: string
| ItemNotFound of Id: Guid
| InvalidPassword
