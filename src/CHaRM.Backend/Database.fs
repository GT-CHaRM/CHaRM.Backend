module CHaRM.Backend.Database

open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.EntityFrameworkCore

open CHaRM.Backend.Model

type ApplicationDbContext (context: DbContextOptions<ApplicationDbContext>) =
    inherit IdentityDbContext<User> (context)

    member val Items: DbSet<ItemType> = Unchecked.defaultof<_> with get, set
    member val Submissions: DbSet<Submission> = Unchecked.defaultof<_> with get, set
