module CHaRM.Util


[<AutoOpen>]
module Types =
    open System
    open System.Threading.Tasks

    type 't obs = IObservable<'t>
    type 't task = Task<'t>

    type Func() =
        static member from (f: unit -> 'a) = Func<'a> f
        static member from (f: 'a -> 'b) = Func<'a, 'b> f
        static member from (f: 'a -> 'b -> 'c) = Func<'a, 'b, 'c> f
        static member from (f: 'a -> 'b -> 'c -> 'd) = Func<'a, 'b, 'c, 'd> f
        static member from (f: 'a -> 'b -> 'c -> 'd -> 'e) = Func<'a, 'b, 'c, 'd, 'e> f

    let (|Assignable|_|) (typeToCheck: Type) (input: Type) =
        if typeToCheck.IsAssignableFrom input then
            Some Assignable
        else
            None

    let log prefix input = printfn "[%s] %O" prefix input; input

module Validation =
    open System.Collections.Concurrent

    open GraphQL.Types
    open GraphQL.Validation


    open Option

    let metadataName field arg = sprintf "$$Field$$%s$$Arg$$%s$$" field arg
    let metadataValueName field arg = sprintf "$$Field$$%s$$ArgValue$$%s$$" field arg

    let setValidationValue (ctx: ValidationContext) name value =
        match ctx.UserContext with
        | :? UserContext as userCtx -> userCtx.Set name value
        | _ -> ()

    let getValidationValue<'t> userCtx name =
        match box userCtx with
        | :? UserContext as userCtx -> userCtx.Get<'t> name
        | _ -> None

    let metadata<'t> (metadataOperation: IObjectGraphType option) name =
        match metadataOperation with
        | Some op -> op.GetMetadata<'t option> (name, None)
        | None -> None

[<AutoOpen>]
module FieldBuilderExtensions =
    open FSharp.Control.Reactive
    open GraphQL.Builders
    open GraphQL.Types

    let resolveAsync
        (this: FieldBuilder<'source, 'retn>)
        (resolve: (ResolveFieldContext<'source> -> 'retn task)) =
        this.ResolveAsync (fun ctx -> resolve ctx |> Task.withError ctx.Errors.Add)

    let resolveObservableList this resolve = resolve >> Observable.toTaskList |> resolveAsync this
    let resolveObservable this resolve = resolve >> Observable.toTask |> resolveAsync this

module Schema =
    open FSharp.Control.Reactive
    open GraphQL.Types

    open Validation

    let private get<'t> name (ctx: ResolveFieldContext<_>) =
        match getValidationValue<'t> ctx.UserContext (metadataValueName ctx.FieldName name) with
        | Some value -> value
        | None -> ctx.GetArgument<'t> name

    type Arg<'t>() = member __.Item with get name = get<'t> name >> Observable.lift
    type ArgSync<'t>() = member __.Item with get name = get<'t> name

    let arg<'t> = Arg<'t>()
    let argSync<'t> = ArgSync<'t>()
