using FluentValidation;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Endpoints;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Filters;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.Middleware;
using LibraryManagement.Api.Services;
using LibraryManagement.Api.Services.Contracts;
using LibraryManagement.Api.Validators.Categories;
using LibraryManagement.Api.Validators.Members;
using Microsoft.AspNetCore.Identity;
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
builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LibraryDbContext>();
builder.Services.AddScoped<AuthorMapper>();
builder.Services.AddScoped<BookMapper>();
builder.Services.AddScoped<CategoryMapper>();
builder.Services.AddScoped<MemberMapper>();
builder.Services.AddScoped<LoanMapper>();
builder.Services.AddScoped<IValidator<CreateCategoryDto>, CreateCategoryDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateCategoryDto>, UpdateCategoryDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateMemberDto>, UpdateMemberDtoValidator>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<LogActionFilter>();
builder.Services.AddScoped<IAuthorsService, AuthorsService>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<ILoansService, LoansService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseMiddleware<RequestTimingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await DataSeeder.SeedAsync(app.Services);
}

app.MapCategoryEndpoints();
app.MapMemberEndpoints();
app.MapControllers();

app.Run();

