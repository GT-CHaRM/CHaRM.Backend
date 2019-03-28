module rec CHaRM.Backend.Model

(* This module contains the classes that define the model of our app. *)

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Identity

[<CLIMutable>]
type ItemType = {
    Id: Guid
    mutable Description: string
    mutable Name: string
}

[<CLIMutable>]
type ItemSubmissionBatch = {
    Id: Guid
    Item: ItemType
    SubmissionId: Guid
    Count: int
}

[<CLIMutable>]
type Submission = {
    Id: Guid
    Visitor: User
    Submitted: DateTimeOffset
    mutable Items: ItemSubmissionBatch HashSet
    mutable ZipCode: string
}

type UserType =
| Visitor = 0
| Employee = 1
| Administrator = 2

type User () =
    inherit IdentityUser<Guid> ()

    member val Type: UserType = UserType.Visitor with get, set
    member val ZipCode: string = null with get, set
    member val Submissions: Submission HashSet = Unchecked.defaultof<_> with get, set
