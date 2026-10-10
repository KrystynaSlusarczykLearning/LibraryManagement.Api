using LibraryManagement.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.DataAccess;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

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

            var fiction = categories.First(category => category.Name == "Fiction");
            var fantasy = categories.First(category => category.Name == "Fantasy");
            var nonFiction = categories.First(category => category.Name == "Non-Fiction");

            var orwell = authors.First(author => author.LastName == "Orwell");
            var austen = authors.First(author => author.LastName == "Austen");
            var tolkien = authors.First(author => author.LastName == "Tolkien");
            var hemingway = authors.First(author => author.LastName == "Hemingway");

            var books = new List<Book>
            {
                new Book
                {
                    Title = "1984",
                    TotalCopies = 5,
                    AvailableCopies = 5,
                    CategoryId = fiction.Id,
                    Authors = [orwell]
                },
                new Book
                {
                    Title = "Pride and Prejudice",
                    TotalCopies = 4,
                    AvailableCopies = 3,
                    CategoryId = fiction.Id,
                    Authors = [austen]
                },
                new Book
                {
                    Title = "The Lord of the Rings",
                    TotalCopies = 6,
                    AvailableCopies = 6,
                    CategoryId = fantasy.Id,
                    Authors = [tolkien]
                },
                new Book
                {
                    Title = "A Farewell to Arms",
                    TotalCopies = 3,
                    AvailableCopies = 2,
                    CategoryId = fiction.Id,
                    Authors = [hemingway]
                },
                new Book
                {
                    Title = "Animal Farm",
                    TotalCopies = 4,
                    AvailableCopies = 4,
                    CategoryId = fiction.Id,
                    Authors = [orwell]
                }
            };

            await dbContext.Books.AddRangeAsync(books);
            
            await dbContext.SaveChangesAsync();
        }

        const string AdminEmail = "admin@library.com";
        if(!await userManager.Users.AnyAsync(user => user.UserName == AdminEmail))
        {
            var librarian = new ApplicationUser
            {
                Email = AdminEmail,
                UserName = AdminEmail
            };

            await userManager.CreateAsync(librarian, "Admin123!");
            await userManager.AddToRoleAsync(librarian, Roles.Librarian);
        }
    }
}
