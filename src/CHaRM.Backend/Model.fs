module CHaRM.Backend.Model

(* This module contains the classes that define the model of our app. *)

open System
open GraphQL.FSharp
open Microsoft.AspNetCore.Identity

/// A single item type that can be submitted to CHaRM.
/// NOTE: This is used to provide future functionality for allowing admins to add new "item types", rather than hardcoding in our types.
[<Auto; CLIMutable>]
type ItemType = {
    Id: Guid
    Name: string
}

/// A batch of a single item type that is submitted during 1 visit.
/// e.g. 5 TVs
[<Auto; CLIMutable>]
type ItemSubmissionBatch = {
    Item: ItemType
    Count: int
}

/// The set of all item submissions during a single visit.
[<Auto; CLIMutable>]
type Submission = {
    Id: Guid
    Submitted: DateTimeOffset
    Items: ItemSubmissionBatch list // TODO: Check
    ZipCode: string
}


type ApplicationUser () =
    inherit IdentityUser ()

    member val ZipCode: string option = None with get, set

[<Auto; AbstractClass>]
type User() =
    member val Id: Guid = Guid.Empty with get, set
    member val Email: string = "" with get, set
    member val Name: string = "" with get, set
    member val InviteAccepted: bool = false with get, set
    member val EmailConfirmed: bool = false with get, set
    member val DeletedAt: DateTimeOffset option = None with get, set
    member val LastLogin: DateTimeOffset option = None with get, set
    member val JoinedAt: DateTimeOffset = DateTimeOffset.MinValue with get, set
    member val IsSuper: bool = false with get, set
    member val SendMail: bool = false with get, set

/// CHaRM Vistior
[<Auto>]
type Visitor() =
    inherit User()

    member val Submissions: Submission list = [] with get, set

/// CHaRM Employee
[<Auto>]
type Employee() =
    inherit User()

/// CHaRM Administrator
[<Auto>]
type Administrator() =
    inherit User()
