using LibraryManagement.Api.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;

namespace LibraryManagement.IntegrationTests.Utils;

internal abstract class IntegrationTestsBase
{
    protected abstract string DatabaseName { get; }
    protected virtual string BaseUrl => string.Empty;
    protected WebApplicationFactory<Program> Factory;
    protected HttpClient Client;
    protected TestDataFactory TestDataFactory;

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
                        options.UseInMemoryDatabase(DatabaseName));
                });
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

    protected async Task<HttpResponseMessage> DeleteAsync(int id)
    {
        return await Client.DeleteAsync($"{BaseUrl}/{id}");
    }
}
