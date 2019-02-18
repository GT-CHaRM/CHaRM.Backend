module CHaRM.Backend.Model

(* This module contains the classes that define the model of our app. *)

open System
open Microsoft.AspNetCore.Identity

/// A single item type that can be submitted to CHaRM.
/// NOTE: This is used to provide future functionality for allowing admins to add new "item types", rather than hardcoding in our types.
[<CLIMutable>]
type ItemType = {
    Id: Guid
    Name: string
}

/// A batch of a single item type that is submitted during 1 visit.
/// e.g. 5 TVs
[<CLIMutable>]
type ItemSubmissionBatch = {
    Item: ItemType
    Count: int
}

/// The set of all item submissions during a single visit.
[<CLIMutable>]
type Submission = {
    Id: Guid
    Submitted: DateTimeOffset
    Items: ItemSubmissionBatch []
    ZipCode: string
}

// Any user
type User () =
    inherit IdentityUser ()

    member val ZipCode: string = null with get, set

/// CHaRM Vistior
type Visitor() =
    inherit User ()

    member val Submissions: Submission [] = [||] with get, set

/// CHaRM Employee
type Employee() =
    inherit User ()

/// CHaRM Administrator
type Administrator() =
    inherit User ()
