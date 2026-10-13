using System.Net;
using PactNet.Matchers;
using PactNet;
using System.Text.Json;

namespace CustomerConsumerNew.Tests;

public sealed class CustomerContractTests
{
    private readonly IPactBuilderV4 _pactBuilder;

    public CustomerContractTests()
    {
        var pactDirectory = GetPactDirectory();
        Directory.CreateDirectory(pactDirectory);

        var pact = Pact.V4(
                "CustomerConsumerNew",
                "CustomerApi",
                new PactConfig
                {
                    PactDir = pactDirectory,
                    LogLevel = PactLogLevel.Information
                });

        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetCustomer_NewConsumerContract()
    {
        // 1. Both email and state is omitted
        _pactBuilder
            .UponReceiving("Customer with lastname omitted and no provider-state")
                .WithRequest(HttpMethod.Get, "/api/customers/101")
                .WithHeader("Accept", "application/json")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader(
                    "Content-Type",
                    "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = Match.Integer(101),
                    firstName = Match.Type("Ava"),
                    email = Match.Type("liam@example.com"),
                    state = "Approved"
                });

        
        await _pactBuilder.VerifyAsync(async context =>
        {
            using var client = new HttpClient
            {
                BaseAddress = context.MockServerUri
            };
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            // Exercise every interaction registered above.
            await AssertCustomer(client, "/api/customers/101");
        });
    }

    private static string GetPactDirectory()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../pacts"));
    }

    private static async Task AssertCustomer(
        HttpClient client,
        string path)
    {
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var root = document.RootElement;

        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("firstName", out _));
        Assert.True(root.TryGetProperty("email", out _));
        Assert.True(root.TryGetProperty("state", out _));
    }
}