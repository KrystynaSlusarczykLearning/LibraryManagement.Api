using LibraryManagement.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.DataAccess;

public class LibraryDbContext : IdentityDbContext<ApplicationUser>
{
    public LibraryDbContext(
        DbContextOptions<LibraryDbContext> options) : base(options)
    {
        
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder
            .Entity<Member>()
            .HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Member>(member => member.UserId);

        builder
            .Entity<IdentityRole>()
            .HasData(
                new IdentityRole
                {
                    Id = "bf542e02-37f0-4fe8-807c-73eed1c09356",
                    Name = Entities.Roles.Member,
                    NormalizedName = Entities.Roles.Member.ToUpper(),
                    ConcurrencyStamp = "01def283-aa36-4b88-b2ea-a053cab46da2"
                },
                new IdentityRole
                {
                    Id = "132d1719-e810-4f6d-9fc1-72781b08b0d7",
                    Name = Entities.Roles.Librarian,
                    NormalizedName = Entities.Roles.Librarian.ToUpper(),
                    ConcurrencyStamp = "5647f8af-22fb-4e7b-ae18-c0fb035407e9"
                });
    }
}
