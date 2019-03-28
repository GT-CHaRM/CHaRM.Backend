module rec CHaRM.Backend.Model

(* This module contains the classes that define the model of our app. *)

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
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
    ItemId: Guid
    [<ForeignKey "ItemId">]
    Item: ItemType
    SubmissionId: Guid
    Count: int
}

[<CLIMutable>]
type Submission = {
    Id: Guid
    VisitorId: Guid
    [<ForeignKey "VisitorId">]
    Visitor: User
    Submitted: DateTimeOffset
    [<ForeignKey "SubmissionId">]
    mutable Items: ItemSubmissionBatch ResizeArray
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
    [<ForeignKey "VisitorId">]
    member val Submissions: Submission ResizeArray = ResizeArray [] with get, set
