module rec CHaRM.Model

open System
open System.Collections.Generic
open GraphQL.FSharp.Builder

[<CLIMutable>]
type ItemType = {
    Id: Guid
    Name: string
}

module ItemType =
    let id (itemType: ItemType) = itemType.Id
    let name (itemType: ItemType) = itemType.Name

[<CLIMutable>]
type ItemInput = {
    Id: Guid
    Name: string
    Count: int
}

module ItemInput =
    let create id count name = {Id = id; Count = count; Name = name;}
    let id (itemInput: ItemInput) = itemInput.Id
    let count (itemInput: ItemInput) = itemInput.Count
    let name (itemInput: ItemInput) = itemInput.Name
    let toMap (seq: #seq<ItemInput>) =
        seq |> Seq.groupBy id |> Seq.fold (fun acc (key, value) -> Map.add key (Seq.length value) acc) Map.empty

    let gqlType = object {
        name "Item"
        description "Item submitted to CHaRM"
        get ()
    }

[<CLIMutable>]
type Submission = {
    Id: Guid
    Submitted: DateTimeOffset
    Items: ItemInput List // TODO: Check
}

module Submission =
    let id (submission: Submission) = submission.Id
    let submitted (submission: Submission) = submission.Submitted
    let items (submission: Submission) = submission.Items

type UserCategory =
| VisitorUser = 0
| EmployeeUser = 1
| AdministratorUser = 2

[<AbstractClass>]
type User() =
    member val Id: Guid = Guid.Empty with get, set
    member val Email: string = "" with get, set
    member val Name: string = "" with get, set
    member val Category: UserCategory = UserCategory.VisitorUser with get, set
    member val InviteAccepted: bool = false with get, set
    member val EmailConfirmed: bool = false with get, set
    member val DeletedAt: DateTimeOffset option = None with get, set
    member val LastLogin: DateTimeOffset option = None with get, set
    member val JoinedAt: DateTimeOffset = DateTimeOffset.MinValue with get, set
    member val IsSuper: bool = false with get, set
    member val SendMail: bool = false with get, set

module User =
    let id (user: User) = user.Id
    let email (user: User) = user.Email
    let name (user: User) = user.Name
    let category (user: User) = user.Category
    let inviteAccepted (user: User) = user.InviteAccepted
    let emailConfirmed (user: User) = user.EmailConfirmed
    let deletedAt (user: User) = user.DeletedAt
    let lastLogin (user: User) = user.LastLogin
    let joinedAt (user: User) = user.JoinedAt
    let isSuper (user: User) = user.IsSuper
    let sendMail (user: User) = user.SendMail

type Visitor() =
    inherit User()
    member val Submissions: Submission List = List() with get, set

module Visitor =
    let submissions (visitor: Visitor) = visitor.Submissions

type Employee() =
    inherit User()

type Administrator() =
    inherit User()

type ErrorCode =
| EmptyString

| StringMinLength of minLength: int
| StringMaxLength of maxLength: int

| ContainsSpaces

| DoesNotContainAll of list: string list
| DoesNotContainAny of list: string list

| UnmatchedRegex of patternName: string

| InvalidInteger

| IntegerMinValue of min: int
| IntegerMaxValue of max: int

| MustBeNDigits of n: int

let (|Visitor|Employee|Administrator|) (user: #User) =
    match user :> User with
    | :? Visitor as visitor when visitor.Category = UserCategory.VisitorUser -> Visitor visitor
    | :? Employee as employee when employee.Category = UserCategory.EmployeeUser -> Employee employee
    | :? Administrator as administrator when administrator.Category = UserCategory.AdministratorUser -> Administrator administrator
    | _ -> failwith "Invalid user type"
