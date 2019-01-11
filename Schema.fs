module rec CHaRM.Schema

open System
open System.Collections.Generic
open FSharp.Control.Reactive
open GraphQL
open GraphQL.Server.Transports.Subscriptions.Abstractions
open GraphQL.Types

open Model
open Providers
open Schema
open Util

open Observable.Operators
open Util.Schema
open Workflow

module Validation =
    open System.Threading.Tasks
    open GraphQL.Execution
    open GraphQL.Validation
    open GraphQL.Language.AST

    open Validation
    open Util.Option

    let enterLeaveListener (f: EnterLeaveListener -> unit) =
        EnterLeaveListener(Action<EnterLeaveListener> f) :> INodeVisitor

    let reportError (ctx: ValidationContext) (arg: Argument) (error: ErrorCode) =
        ctx.ReportError(ValidationError(ctx.OriginalQuery, ErrorCode.code error, ErrorCode.stringify error, arg.NamedNode))

    let handleErrors ctx (errors: (ErrorCode * Argument) list) =
        for (err, arg) in errors do reportError ctx arg err

    type GenericValidationType() =
        interface IValidationRule with
            member __.Validate ctx =
                enterLeaveListener (fun listener ->
                    let mutable metadataOperation: IObjectGraphType option = None
                    listener.Match<Operation>(enter = fun operation ->
                        match operation.OperationType with
                        | OperationType.Query -> metadataOperation <- Some ctx.Schema.Query
                        | OperationType.Mutation -> metadataOperation <- Some ctx.Schema.Mutation
                        | _ -> ())
                    let mutable currentField: Field option = None
                    listener.Match<Field>(enter = fun field -> currentField <- Some field)
                    listener.Match<Argument>(enter = fun arg ->
                        ! (maybe {
                            let! field = currentField
                            let! validator =
                                metadataName field.Name arg.Name
                                |> metadata<Validator<obj, ErrorCode>> metadataOperation
                            match validator arg.Value.Value with
                            | Ok value -> (ctx.UserContext :?> UserContext).Set (metadataValueName field.Name arg.Name) value
                            | Error errors -> List.iter (reportError ctx arg) errors
                        })))

// [<AutoOpen>]
// module User =
//     let userContext (ctx: ResolveFieldContext<_>) = ctx.UserContext :?> GraphQLUserContext
//     let user ctx =
//         let userCtx = userContext ctx
//         userCtx.Get

type ItemTypeType() as this =
    inherit ObjectGraphType<ItemType>()

    do this |> object {
        name "Item"
        description "A specific type of item that is submitted to our facility"
        field (f {
            get (fun item -> item.Id)
            description "Id of the item"
        })
        field (f {
            get (fun item -> item.Name)
            description "Name of the item type"
        })
    }

let item = object {
    name "Item"
    description "A specific type of item that is submitted to our facility"
    field (f {
        get (fun item -> item.Id)
        description "Id of the item"
    })
    field (f {
        get (fun item -> item.Name)
        description "Name of the item type"
    })
}

type ItemInputType() as this =
    inherit ObjectGraphType<ItemInput>()

    do this |> object {
        name "ItemInput"
        description "" // TODO:
        field (f {
            get (fun input -> input.Id)
        })
        field (f {
            get (fun input -> input.Name)
        })
        field (f {
            get (fun input -> input.Count)
        })
    }

type SubmissionType() as this =
    inherit ObjectGraphType<Submission>()

    do this |> object {
        name "Submission"
        description "A set of items being submitted during a single visit to CHaRM"
        field (f {
            get (fun submission -> submission.Id)
            description "The ID of the submission"
        })
        field (f {
            get (fun submission -> submission.Submitted)
            description "The time of submission"
        })
        field (f {
            getType (fun submission -> submission.Items) typeof<ListGraphType<ItemInputType>>
            description "The items being submitted"
        })
    }

type UserCategoryType() =
    inherit EnumerationGraphType<UserCategory>()

let registerUserFields<'user, 'graph when 'user :> User and 'graph :> ComplexGraphType<'user>> (this: 'graph) =
    this |> complex {
        field (f {
            get (fun user -> user.Id)
            description "Id of the user"
        })
        field (f {
            get (fun user -> user.Email)
            description "Email of the user"
        })
        field (f {
            get (fun user -> user.Name)
            description "Name of the user"
        })
        field (f {
            getType (fun user -> user.Category) typeof<UserCategoryType>
            description "Category of the user"
        })
        field (f {
            get (fun user -> user.InviteAccepted)
            description "InviteAccepted of the user"
        })
        field (f {
            get (fun user -> user.EmailConfirmed)
            description "EmailConfirmed of the user"
        })
        field (f {
            name "deletedAt"
            get (fun user -> Option.toNullable user.DeletedAt)
            nullable
            description "DeletedAt of the user"
        })
        field (f {
            name "lastLogin"
            get (fun user -> Option.toNullable user.LastLogin)
            nullable
            description "LastLogin of the user"
        })
        field (f {
            get (fun user -> user.JoinedAt)
            description "JoinedAt of the user"
        })
        field (f {
            get (fun user -> user.IsSuper)
            description "IsSuper of the user"
        })
        field (f {
            get (fun user -> user.SendMail)
            description "SendMail of the user"
        })
    }

type UserType() as this =
    inherit InterfaceGraphType<User>()

    do this |> complex {
        name "User"
        description "A base type for all users in this system"
        import registerUserFields
    }

let userBase<'user, 'graph when 'user :> User and 'graph :> ObjectGraphType<'user>> (this: 'graph) =
    registerUserFields<'user, 'graph> this
    this.Interface<UserType>()
    this.IsTypeOf <- (fun obj ->
        match obj :?> User with
        | Visitor _ -> typeof<'user> = typeof<Visitor>
        | Employee _ -> typeof<'user> = typeof<Employee>
        | Administrator _ -> typeof<'user> = typeof<Administrator>)

type VisitorType() as this =
    inherit ObjectGraphType<Visitor>()

    do this |> object {
        name "Visitor"
        description "A visitor of CHaRM"
        import userBase
        field (f {
            description "Submissions of the user"
            getType (fun visitor -> visitor.Submissions) typeof<ListGraphType<SubmissionType>>
        })
    }

type EmployeeType() as this =
    inherit ObjectGraphType<Employee>()

    do this |> object {
        name "Employee"
        description "An employee of CHaRM"
        import userBase
    }

type AdministratorType() as this =
    inherit ObjectGraphType<Administrator>()

    do this |> object {
        name "Administrator"
        description "An administrator of CHaRM"
        import userBase
    }

module Query =
    let user (users: UserProvider) = object {
        field (f<ListGraphType<UserType>, List<User>, _> {
            name "users"
            resolve (ignore >> users.All)
        })
        field (f<UserType, User, _> {
            name "currentUser"
            resolve (fun i -> Visitor() :> User)
        })
        field (f<UserType, User, _> {
            name "user"
            argument (a<IdGraphType, Guid> {
                name "id"
                description "The id of the user to get."
            })
            resolve (
                arg<Guid>.["id"]
                >=> users.Get
                >> Observable.throwIf (ExecutionError "Invalid username") Observable.isEmpty
            )
        })
    }

    let item (items: ItemProvider) = object {
        field (f<ListGraphType<ItemTypeType>, List<ItemType>, _> {
            name "items"
            resolve (ignore >> items.All)
        })
    }

    let submission (submissions: SubmissionProvider) = object {
        field (f<ListGraphType<SubmissionType>, List<Submission>, _> {
            name "submissions"
            resolve (ignore >> submissions.All)
        })
        field (f<SubmissionType, Submission, _> {
            name "submission"
            argument (a<IdGraphType, Guid> {
                name "id"
                description "The id of the submission to get."
            })
            resolve (arg<Guid>.["id"] >=> submissions.Get)
        })
    }

module Mutation =
    let user (users: UserProvider) = object {
        field (f<_,_,_> {
            name "login" // TODO
        })
        field (f<_,_,_> {
            name "register" // TODO
        })
    }

let itemMutation (items: ItemProvider) = object {
    field (f<ItemTypeType, ItemType, _> {
        name "newItem"
        argument (a<StringGraphType, string> {
            name "name"
            validator (Validation.Model.ItemType.validateName)
            description "The name of the item to add."
        })
        resolve (arg<string>.["name"] >=> items.Create)
    })
}

    let submission (submissions: SubmissionProvider) = object {
        field (f<SubmissionType, Submission, _> {
            name "newSubmission"
            argument (a<IdGraphType ListGraphType, List<Guid>> {
                name "items"
                description "The items of the submission"
            })
            resolve (arg<List<Guid>>.["items"] >=> submissions.Create)
        })
    }

type Query(dependencyResolver: IDependencyResolver) as this =
    inherit ObjectGraphType<obj>()

    let items = dependencyResolver.Resolve<ItemProvider>()
    let users = dependencyResolver.Resolve<UserProvider>()
    let submissions = dependencyResolver.Resolve<SubmissionProvider>()

    do this |> object {
        name "Query"
        description "Requests by client"
        import (Query.user users)
        import (Query.item items)
        import (Query.submission submissions)
    }

type Mutation(dependencyResolver: IDependencyResolver) as this =
    inherit ObjectGraphType<obj>()

    let users = dependencyResolver.Resolve<UserProvider>()
    let items = dependencyResolver.Resolve<ItemProvider>()
    let submissions = dependencyResolver.Resolve<SubmissionProvider>()

    do this |> object {
        name "Mutation"
        description "Mutations by client"
        // import (Mutation.user users)
        import (Mutation.item items)
        import (Mutation.submission submissions)
    }

type Schema(dependencyResolver) as this =
    inherit GraphQL.Types.Schema(dependencyResolver = dependencyResolver)

    do
        this.Query <- Query(dependencyResolver)
        this.Mutation <- Mutation(dependencyResolver)
        this.RegisterType<VisitorType>()
        this.RegisterType<EmployeeType>()
        this.RegisterType<AdministratorType>()
