using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Users;
using LibraryManagement.Api.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LibraryManagement.IntegrationTests.Utils;

internal abstract class IntegrationTestsBase
{
    protected abstract string DatabaseName { get; }
    protected virtual string BaseUrl => string.Empty;
    protected WebApplicationFactory<Program> Factory;
    protected HttpClient Client;
    protected TestDataFactory TestDataFactory;
    private const string LibrarianEmail = "librarian@library.com";
    private const string LibrarianPassword = "Librarian567@";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDbContextOptionsConfiguration<LibraryDbContext>>();
                    services.RemoveAll<LibraryDbContext>();

                    services.AddDbContext<LibraryDbContext>(options =>
                        options
                        .UseInMemoryDatabase(DatabaseName)
                        .ConfigureWarnings(warning => 
                            warning.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                    services.Configure<PasswordHasherOptions>(
                        options => options.IterationCount = 1);
                });
                builder.UseSetting(
                    "JwtSettings:Key",
                    "integration-tests-signing-key-at-least-32-bytes-long");
            });

        Client = Factory.CreateClient();

        TestDataFactory = new TestDataFactory(Client);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Client.Dispose();
        Factory.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        using var scope = Factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        await CreateLibrarianAsync();
        var librarianToken = await LoginAsync(LibrarianEmail, LibrarianPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", librarianToken);
    }

    protected async Task<HttpResponseMessage> GetByIdAsync(int id)
    {
        return await Client.GetAsync($"{BaseUrl}/{id}");
    }

    protected async Task<HttpResponseMessage> GetAllAsync()
    {
        return await Client.GetAsync(BaseUrl);
    }

    protected async Task<HttpResponseMessage> GetWithQuery(
        string uri,
        Dictionary<string, string?> queryParameters)
    {
        var url = QueryHelpers.AddQueryString(uri, queryParameters);

        return await Client.GetAsync(url);
    }

    protected async Task<TReadDto> CreateAsync<TCreateDto, TReadDto>(
        TCreateDto createDto)
    {
        var createResponse = await PostAsync(createDto);

        await createResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        return (await createResponse.ReadPayloadAsync<TReadDto>())!;
    }

    protected async Task<HttpResponseMessage> PostAsync<T>(T createDto)
    {
        return await Client.PostAsJsonAsync(BaseUrl, createDto);
    }

    protected async Task<HttpResponseMessage> PutAsync<T>(T updateDto, int id)
    {
        return await Client.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);
    }

    protected async Task<HttpResponseMessage> PatchWithoutBodyAsync(string uri)
    {
        return await Client.PatchAsync(uri, null);
    }

    protected async Task<HttpResponseMessage> JsonPatchAsync(
        int id,
        params JsonPatchOperation[] operations)
    {
        var content = JsonContent.Create(
            operations,
            new MediaTypeHeaderValue("application/json-patch+json"));

        return await Client.PatchAsync($"{BaseUrl}/{id}", content);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(int id)
    {
        return await Client.DeleteAsync($"{BaseUrl}/{id}");
    }

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email, string password)
    {
        var token = await LoginAsync(email, password);

        var authenticatedClient = Factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return authenticatedClient;
    }

    private async Task CreateLibrarianAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await roleManager.CreateAsync(new IdentityRole(Roles.Librarian));
        await roleManager.CreateAsync(new IdentityRole(Roles.Member));

        var librarian = new ApplicationUser
        {
            Email = LibrarianEmail,
            UserName = LibrarianEmail
        };

        await userManager.CreateAsync(librarian, LibrarianPassword);
        await userManager.AddToRoleAsync(librarian, Roles.Librarian);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/authentication/login", loginDto);

        await loginResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        var token = await loginResponse.Content.ReadAsStringAsync();
        return token;
    }
}
