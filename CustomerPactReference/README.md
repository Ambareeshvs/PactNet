# Customer Pact reference

This .NET 10 sample demonstrates consumer-driven contract testing with PactNet
and xUnit.

## Projects

- `CustomerApi/` is the provider API. `GET /customers/{id}` returns a seeded
  customer or `404 Not Found`; `POST /provider-states` prepares data for Pact
  provider verification.
- `CustomerConsumer.Tests/` defines the consumer expectation and writes
  `pacts/CustomerConsumer-CustomerApi.json`.
- `CustomerConsumerNew.Tests/` defines the another consumer expectation and writes
  `pacts/CustomerConsumerNew-CustomerApi.json`.
- `CustomerProvider.Tests/` starts the actual API on an ephemeral local port and
  verifies the checked-in Pact against it.

## Run

From this directory, run the consumers test first to regenerate the Pact, then
verify the provider:

```sh
dotnet test CustomerConsumer.Tests/CustomerConsumer.Tests.csproj
dotnet test CustomerConsumerNew.Tests/CustomerConsumerNew.Tests.csproj
dotnet test CustomerProvider.Tests/CustomerProvider.Tests.csproj
```

Run the API independently with:

```sh
dotnet run --project CustomerApi/CustomerApi.csproj
```

The generated launch profiles use `http://localhost:5202` and
`https://localhost:7274`.
