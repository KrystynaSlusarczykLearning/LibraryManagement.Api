using FluentValidation;
using LibraryManagement.Api.Common;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

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
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings")
    .Get<JwtSettings>();
if(jwtSettings is null)
{
    throw new InvalidOperationException("JwtSettings section is not configured.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Authentication is required to access this resource.",
                    Instance = context.Request.Path
                };

                return context.Response.WriteAsJsonAsync(
                    problemDetails,
                    options: null,
                    contentType: "application/problem+json");
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Roles.Librarian, policy => policy.RequireRole(Roles.Librarian));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
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

