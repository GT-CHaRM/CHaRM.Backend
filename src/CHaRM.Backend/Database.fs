module CHaRM.Backend.Database

#nowarn "44"

open System
open System.Threading.Tasks
open FSharp.Utils.Tasks
open FSharp.Utils.Tasks.TplPrimitives
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.EntityFrameworkCore

open CHaRM.Backend.Model

type ContinuationTaskBuilder (cont: unit -> Task) =
    inherit AwaitableBuilder ()

    member __.Run (f : unit -> Ply<'u>) =
        task {
            let! value = f ()
            do! cont ()
            return value
        }

type ApplicationDbContext (context: DbContextOptions<ApplicationDbContext>) =
    inherit IdentityDbContext<User, IdentityRole<Guid>, Guid> (context)

    [<DefaultValue>]
    val mutable items: DbSet<ItemType>

    member this.Items
        with get () = this.items
        and set value = this.items <- value

    [<DefaultValue>]
    val mutable submissions: DbSet<Submission>

    member this.Submissions
        with get () = this.submissions
        and set value = this.submissions <- value

    member this.changes =
        ContinuationTaskBuilder (
            fun () ->
                unitTask {
                    let! _ = this.SaveChangesAsync Unchecked.defaultof<_>
                    ()
                }
        )
