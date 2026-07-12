using LibraryManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.DataAccess;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        if(!await dbContext.Authors.AnyAsync())
        {
            var authors = new List<Author>
            {
                new Author { FirstName = "George", LastName = "Orwell", YearOfBirth = 1903 },
                new Author { FirstName = "Jane", LastName = "Austen", YearOfBirth = 1775 },
                new Author { FirstName = "Mark", LastName = "Twain", YearOfBirth = 1835 },
                new Author { FirstName = "J.R.R.", LastName = "Tolkien", YearOfBirth = 1892 },
                new Author { FirstName = "Ernest", LastName = "Hemingway", YearOfBirth = 1899 }
            };

            await dbContext.Authors.AddRangeAsync(authors);
            await dbContext.SaveChangesAsync();

            var categories = new List<Category>
            {
                new Category { Name = "Fiction" },
                new Category { Name = "Non-Fiction" },
                new Category { Name = "Science Fiction" },
                new Category { Name = "Fantasy" },
                new Category { Name = "Biography" }
            };

            await dbContext.Categories.AddRangeAsync(categories);
            await dbContext.SaveChangesAsync();
        }
    }
}
