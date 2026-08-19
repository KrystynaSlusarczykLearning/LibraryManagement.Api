using LibraryManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.DataAccess;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(
        DbContextOptions<LibraryDbContext> options) : base(options)
    {
        
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
}
