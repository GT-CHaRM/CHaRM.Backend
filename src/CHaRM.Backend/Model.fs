module rec CHaRM.Backend.Model

(* This module contains the classes that define the model of our app. *)

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.AspNetCore.Identity

[<CLIMutable>]
type ItemType = {
    Id: Guid
    Name: string
}

[<CLIMutable>]
type ItemSubmissionBatch = {
    Id: Guid
    ItemKey: Guid
    [<ForeignKey "ItemKey">]
    Item: ItemType
    Count: int
}

[<CLIMutable>]
type Submission = {
    Id: Guid
    VisitorKey: Guid
    [<ForeignKey "VisitorKey">]
    Visitor: User
    Submitted: DateTimeOffset
    [<ForeignKey "ItemKey">]
    Items: ItemSubmissionBatch []
    ZipCode: string
}

type UserType =
| Visitor = 0
| Employee = 1
| Administrator = 2

type User () =
    inherit IdentityUser<Guid> ()

    member val Type: UserType = UserType.Visitor with get, set
    member val ZipCode: string = null with get, set
    member val Submissions: Submission [] = [||] with get, set
