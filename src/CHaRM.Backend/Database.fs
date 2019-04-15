module CHaRM.Backend.Database

#nowarn "44"

open System
open System.Threading.Tasks
open FSharp.Utils.Reflection
open FSharp.Utils.Tasks
open FSharp.Utils.Tasks.TplPrimitives
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage.ValueConversion

open CHaRM.Backend.Model

module Conversion =
    open Microsoft.FSharp.Linq.RuntimeHelpers
    open System
    open System.Linq.Expressions

    let toOption<'t> =
        <@ Func<'t, 't option>(fun (x : 't) -> match box x with null -> None | _ -> Some x) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<'t, 't option>>>

    let fromOption<'t> =
        <@ Func<'t option, 't>(fun (x : 't option) -> match x with Some y -> y | None -> Unchecked.defaultof<'t>) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<'t option, 't>>>

type OptionConverter<'t> () =
    inherit ValueConverter<'t option, 't> (Conversion.fromOption, Conversion.toOption)

type ContinuationTaskBuilder (cont: unit -> Task) =
    inherit AwaitableBuilder ()

    member __.Run (f : unit -> Ply<'u>) =
        vtask {
            let! value = f ()
            do! cont ()
            return value
        }

type ApplicationDbContext (context: DbContextOptions<ApplicationDbContext>) =
    inherit IdentityDbContext<User, IdentityRole<Guid>, Guid> (context)

    override __.OnConfiguring builder =
        builder.EnableSensitiveDataLogging ()
        |> ignore
        base.OnConfiguring builder

    override __.OnModelCreating builder =
        base.OnModelCreating builder
        builder.Model.GetEntityTypes ()
        |> Seq.collect (fun entity -> entity.GetProperties ())
        |> Seq.iter (
            fun property ->
                match property.ClrType with
                | OptionType ``type`` ->
                    typedefof<OptionConverter<_>>
                        .MakeGenericType(``type``)
                        .GetConstructor([||])
                        .Invoke([||])
                    |> unbox<ValueConverter>
                    |> property.SetValueConverter
                | _ -> ()
        )

    [<DefaultValue>]
    val mutable private items: DbSet<ItemType>

    member this.Items
        with get () = this.items
        and set value = this.items <- value

    [<DefaultValue>]
    val mutable private submissions: DbSet<Submission>

    member this.Submissions
        with get () = this.submissions
        and set value = this.submissions <- value

    member this.changes = ContinuationTaskBuilder (fun () -> unitTask { do! this.SaveChangesAsync Unchecked.defaultof<_> :> Task })
