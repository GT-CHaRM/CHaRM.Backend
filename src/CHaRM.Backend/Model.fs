module CHaRM.Backend.Model

(* This module contains the classes that define the model of our app. *)

open System

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
    Items: ItemSubmissionBatch list // TODO: Check
}

/// Parent interface for every user type.
type IUser =
    abstract member Id: Guid with get, set
    abstract member Email: string with get, set
    abstract member Name: string with get, set
    abstract member InviteAccepted: bool with get, set
    abstract member EmailConfirmed: bool with get, set
    abstract member DeletedAt: DateTimeOffset option with get, set
    abstract member LastLogin: DateTimeOffset option with get, set
    abstract member JoinedAt: DateTimeOffset with get, set
    abstract member IsSuper: bool with get, set
    abstract member SendMail: bool with get, set

[<AbstractClass>]
type User() =
    interface IUser with
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
type Visitor() =
    inherit User()

    member val Submissions: Submission list = [] with get, set

/// CHaRM Employee
type Employee() =
    inherit User()

/// CHaRM Administrator
type Administrator() =
    inherit User()
