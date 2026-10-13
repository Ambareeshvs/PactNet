using System.Net;
using PactNet.Matchers;
using PactNet;
using System.Text.Json;

namespace CustomerConsumer.Tests;

public sealed class CustomerContractTests
{
    private readonly IPactBuilderV4 _pactBuilder;

    public CustomerContractTests()
    {
        var pactDirectory = GetPactDirectory();
        Directory.CreateDirectory(pactDirectory);

        var pact = Pact.V4(
                "CustomerConsumer",
                "CustomerApi",
                new PactConfig
                {
                    PactDir = pactDirectory,
                    LogLevel = PactLogLevel.Information
                });

        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetCustomer_ShouldHonorTheConsumerContract()
    {
        // 1. No query parameter: email null, state null.
        _pactBuilder
            .UponReceiving("Customer has null email and null state")
                .Given("Customer has null email and null state")
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
                    lastName = Match.Type("Shah"),
                    email = (string?)null,
                    state = (string?)null
                });

        // 2. No query parameter: email string, state Approved.
        _pactBuilder
            .UponReceiving("Customer has email and approved state")
                .Given("Customer has email and approved state")
                .WithRequest(HttpMethod.Get, "/api/customers/102")
                .WithHeader("Accept", "application/json")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader(
                    "Content-Type",
                    "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = Match.Integer(102),
                    firstName = Match.Type("Noah"),
                    lastName = Match.Type("Patel"),
                    email = Match.Type("noah@example.com"),
                    state = "Approved"
                });

        // 3. emailRqd=true: email is non-null, state null.
        _pactBuilder
            .UponReceiving("Required email and null state")
                .Given("Customer has required email and null state")
                .WithRequest(
                    HttpMethod.Get,
                    "/api/customers/103")
                .WithQuery("emailRqd", "true")
                .WithHeader("Accept", "application/json")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader(
                    "Content-Type",
                    "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = Match.Integer(103),
                    firstName = Match.Type("Mia"),
                    lastName = Match.Type("Jones"),
                    email = Match.Type("mia@example.com"),
                    state = (string?)null
                });

        // 4. emailRqd=true: email is non-null, state Approved.
        _pactBuilder
            .UponReceiving("Required email and approved state")
                .Given("Customer has required email and approved state")
                .WithRequest(
                    HttpMethod.Get,
                    "/api/customers/104")
                .WithQuery("emailRqd","true")
                .WithHeader("Accept", "application/json")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader(
                    "Content-Type",
                    "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = Match.Integer(104),
                    firstName = Match.Type("Liam"),
                    lastName = Match.Type("Brown"),
                    email = Match.Type("liam@example.com"),
                    state = "Approved"
                });

        // 5. emailRqd=false: email null, state Approved.
        _pactBuilder
            .UponReceiving("Optional email is null and state is approved")
                .Given(
                    "Customer has optional null email and approved state")
                .WithRequest(
                    HttpMethod.Get,
                    "/api/customers/105")
                .WithQuery("emailRqd","false")
                .WithHeader("Accept", "application/json")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader(
                    "Content-Type",
                    "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = Match.Integer(105),
                    firstName = Match.Type("Zoe"),
                    lastName = Match.Type("Wilson"),
                    email = (string?)null,
                    state = "Approved"
                });

        // 6. emailRqd=false: email string, state null.
        _pactBuilder
            .UponReceiving("Optional email is present and state is null")
                .Given("Customer has optional email and null state")
                .WithRequest(
                    HttpMethod.Get,
                    "/api/customers/106")
                .WithQuery("emailRqd","false")
                .WithHeader("Accept", "application/json")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader(
                    "Content-Type",
                    "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = Match.Integer(106),
                    firstName = Match.Type("Ethan"),
                    lastName = Match.Type("Davis"),
                    email = Match.Type("ethan@example.com"),
                    state = (string?)null
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
            await AssertCustomer(client, "/api/customers/102");
            await AssertCustomer(client, "/api/customers/103?emailRqd=true");
            await AssertCustomer(client, "/api/customers/104?emailRqd=true");
            await AssertCustomer(client, "/api/customers/105?emailRqd=false");
            await AssertCustomer(client, "/api/customers/106?emailRqd=false");
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
        Assert.True(root.TryGetProperty("lastName", out _));
        Assert.True(root.TryGetProperty("email", out _));
        Assert.True(root.TryGetProperty("state", out _));
    }
}