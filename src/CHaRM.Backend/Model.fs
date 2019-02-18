module CHaRM.Backend.Model

(* This module contains the classes that define the model of our app. *)

open System
open Microsoft.AspNetCore.Identity

[<CLIMutable>]
type ItemType = {
    Id: Guid
    Name: string
}

[<CLIMutable>]
type ItemSubmissionBatch = {
    Id: Guid
    Item: ItemType
    Count: int
}

[<CLIMutable>]
type Submission = {
    Id: Guid
    Submitted: DateTimeOffset
    Items: ItemSubmissionBatch []
    ZipCode: string
}

type User () =
    inherit IdentityUser ()

    member val ZipCode: string = null with get, set
    member val Submissions: Submission [] = [||] with get, set
