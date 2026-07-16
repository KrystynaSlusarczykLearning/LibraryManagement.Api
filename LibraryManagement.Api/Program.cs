using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.Endpoints;
using LibraryManagement.Api.Mapping;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("LibraryDb")));
builder.Services.AddScoped<AuthorMapper>();
builder.Services.AddScoped<CategoryMapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await DataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.MapCategoryEndpoints();
app.MapControllers();

app.Run();

