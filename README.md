# PactNet Reference Project: Consumer-Driven Contract Testing in .NET

A practical reference project demonstrating **consumer-driven contract
testing** with **PactNet** and ASP.NET Core.

The goal of this repository is to explain not only how to write Pact
tests, but also **why they exist, where they fit alongside unit and
integration tests, how provider states work, and when to run contract
verification in a CI/CD pipeline**.

> **Project context:** This sample uses PactNet 5.0.1, an ASP.NET Core
> customer API, and an in-memory/static repository for demonstration.
> Adapt the setup, endpoint behavior, and state handling to your
> application.

------------------------------------------------------------------------

## Table of contents

-   [What is Pact?](#what-is-pact)
-   [Why contract testing matters](#why-contract-testing-matters)
-   [How Pact works](#how-pact-works)
-   [Consumer tests vs provider
    verification](#consumer-tests-vs-provider-verification)
-   [Provider states](#provider-states)
-   [Example API and contract
    scenarios](#example-api-and-contract-scenarios)
-   [Optional fields, nullable fields, and
    matchers](#optional-fields-nullable-fields-and-matchers)
-   [Where Pact fits in the test
    strategy](#where-pact-fits-in-the-test-strategy)
-   [Where Pact fits in CI/CD](#where-pact-fits-in-cicd)
-   [When to use provider states](#when-to-use-provider-states)
-   [Best practices](#best-practices)
-   [Common misunderstandings and
    troubleshooting](#common-misunderstandings-and-troubleshooting)
-   [Suggested repository layout](#suggested-repository-layout)
-   [References](#references)

------------------------------------------------------------------------

## What is Pact?

**Pact is a consumer-driven contract testing tool.** It helps verify
that a service providing an API (the **provider**) continues to meet the
expectations of the applications or services consuming that API (the
**consumers**).

For example:

-   `CustomerConsumer` calls a customer API.
-   `CustomerApi` provides `GET /api/customers/{id}`.
-   The consumer relies on the response containing `id`, `firstName`,
    and `lastName`.
-   A Pact contract records that request and the response behavior the
    consumer expects.
-   Provider verification checks that the real API still satisfies that
    contract.

**PactNet** is the .NET client library used to write Pact consumer tests
and provider verification tests in .NET projects.

Pact is not a replacement for unit tests, integration tests, or
end-to-end tests. It addresses a different question:

> **Can the consumer and provider work together according to the
> contract the consumer relies on, without needing to run the entire
> system for every test?**

## Why contract testing matters

Imagine a provider team changes a response:

Before:

``` json
{
  "id": 101,
  "firstName": "Ava",
  "lastName": "Shah",
  "email": null,
  "state": null
}
```

After a change, the provider renames `firstName` to `givenName`, removes
a field, changes a response status, or changes how a query parameter is
interpreted.

The provider's own unit tests might all pass. However, a consumer may
still fail because it expects the old response shape or behavior.

Contract testing helps detect these compatibility problems **before the
consumer and provider are deployed together**.

Benefits include:

-   **Earlier feedback:** detect contract incompatibilities in CI rather
    than after deployment.
-   **Clear expectations:** make the API behavior relied on by consumers
    explicit.
-   **Independent delivery:** teams can test compatibility without
    launching every dependent service.
-   **Safer refactoring:** verify that provider changes still satisfy
    existing consumer contracts.
-   **Living documentation:** Pact files show the interactions and
    response details consumers actually rely on.

A Pact contract is not a complete API specification. It captures the
interactions covered by consumer tests, not every possible behavior of
the provider.

------------------------------------------------------------------------

## How Pact works

At a high level:

``` text
Consumer code
    |
    v
Consumer Pact test
    |
    | Sends request to a Pact mock server
    | Checks the response expected by the consumer
    v
Pact JSON file
    |
    | Shared with the provider team / Pact Broker
    v
Provider verification test
    |
    | Replays the recorded requests against the real provider API
    | Checks status, headers, and body against the contract
    v
Pass or fail
```

There are two key stages.

### 1. Consumer test: create the contract

The consumer test uses a **Pact mock server**, not the real provider. It
describes a request the consumer makes and the response the consumer
needs.

When the test passes, Pact writes a Pact JSON file containing the
interaction and its expected response.

The consumer test is useful because it checks the consumer's assumptions
and creates an executable contract.

### 2. Provider verification: verify the contract

The provider verification test loads a Pact file and sends the recorded
requests to the **real provider application**.

It checks whether the provider's actual response matches the contract.
If the provider returns a different status, omits a required field, or
returns a mismatched value/type, verification can fail.

Provider verification does not automatically create test data. If a
request refers to customer `101`, the verifier normally sends that
request with ID `101`. A provider-state handler or another deterministic
test setup must make the expected conditions true.

------------------------------------------------------------------------

## Consumer tests vs provider verification

| Concern | Consumer Pact test | Provider verification |
| --- | --- | --- |
| Main question | Does the consumer's expected interaction behave as specified? | Does the real provider satisfy the consumer's contract? |
| Uses | Pact mock server | Real provider API |
| Produces | Pact JSON contract | Verification result |
| Data setup | Usually defines example request/response expectations | Must arrange provider data/state when needed |
| Typical failure | Consumer's expectations or test are inconsistent | Provider response doesn't match the contract, or setup fails |

These are related but different tests. A successful consumer test does
**not** prove the provider implements the contract. Provider
verification provides that check.

------------------------------------------------------------------------

## Provider states

A **provider state** is a named precondition that must be established
before a particular Pact interaction is verified.

For example:

``` csharp
.Given("Customer has null email and null state")
```

This means:

> Before verifying this interaction, arrange the provider so that the
> condition described by this state is true.

Pact does not infer what the state name means. Your provider-state
handler must implement it.

### Why provider states are useful

Suppose the API supports:

``` http
GET /api/customers/101
```

The consumer needs to handle a customer whose `state` is `null`. Another
interaction checks a customer whose state is `"Approved"`.

Those are different preconditions. Provider states let the verification
process establish each condition independently.

| Pact interaction | Provider state | Data arranged before the request |
| --- | --- | --- |
| Customer has null state | `Customer has null email and null state` | Customer `101` has `email = null`, `state = null` |
| Customer has approved state | `Customer has email and approved state` | Customer `102` has an email and `state = "Approved"` |

The provider-state endpoint can reset and seed an in-memory repository,
prepare records in a test database, or configure another controlled
dependency. The exact implementation depends on the application.

### How the provider-state lifecycle works

1.  Provider verification loads an interaction from the Pact file.
2.  If the interaction declares a provider state, the verifier calls the
    configured state-change endpoint with that state.
3.  The handler maps the state name to setup actions.
4.  The verifier sends the interaction's recorded request to the
    provider API.
5.  The verifier compares the actual response with the expected
    response.
6.  The next interaction's state is arranged before that interaction is
    verified.

The provider-state endpoint is **a hook for your setup code**. Pact does
not automatically seed a database or change an API response simply
because a state name appears in `.Given(...)`.

### Does the same customer ID need to be used for every state?

No. Different states can use different IDs, as this sample does.

The same ID can also be reused across separate interactions if the setup
handler resets or replaces that customer's test data before each
interaction. The records do not need to coexist in conflicting states at
the same time.

The important requirement is that each interaction's setup establishes
the preconditions that interaction expects.

### Is `.Given()` mandatory?

No. An interaction without `.Given()` can be verified. But the provider
still needs to return a matching response for the request.

Without a named state, the verifier does not have a specific named
precondition to request. Verification then relies on existing
deterministic test data or another setup mechanism. If the API depends
on data that is missing or unpredictable, verification may fail.

If your verification configuration calls a provider-state endpoint for
interactions without a named state, make sure the endpoint handles that
case intentionally. Do not assume an empty state automatically creates
suitable test data.

### When should provider states be used?

Use provider states when an interaction depends on a particular provider
condition, such as:

-   A customer exists with a null email.
-   A customer exists with an approved state.
-   A resource does not exist and the API should return `404`.
-   A user has the required permissions.
-   A payment or order is in a specific lifecycle state.
-   A feature flag or controlled dependency affects the response.

Provider states are especially helpful when the provider uses a database
or other mutable state.

They may be unnecessary for a fully deterministic provider
implementation that always returns the required response for a given
request. Even then, provider states can make the test setup explicit and
demonstrate a pattern that scales to more realistic services.

### Provider-state safety

-   Use isolated test data or a dedicated test database.
-   Never point verification setup code at production data.
-   Make setup repeatable and, where possible, idempotent.
-   Clean up or reset test data between interactions as appropriate.
-   Avoid running tests in parallel if they mutate the same shared
    static repository or shared records without isolation.
-   Keep provider-state setup code available in the same application
    process or connected to the same backing store the API actually
    reads.

------------------------------------------------------------------------

## Example API and contract scenarios

This reference project demonstrates a customer endpoint conceptually
shaped like:

``` http
GET /api/customers/{id}[?emailRqd=true|false]
```

Example response:

``` json
{
  "id": 101,
  "firstName": "Ava",
  "lastName": "Shah",
  "email": null,
  "state": null
}
```

The sample uses a static/in-memory repository instead of a database so
the Pact concepts are easy to explore.

### Example scenarios

| Scenario | Request | Expected behavior |
| --- | --- | --- |
| Basic customer response | `GET /api/customers/101` | `200` with the customer fields relied on by the consumer |
| Null email and state | `GET /api/customers/101` | `200`, with `email` and `state` null if those fields are included in the expected response |
| Optional email is null | `GET /api/customers/105?emailRqd=false` | `200`; null email is allowed |
| Email is required and present | `GET /api/customers/104?emailRqd=true` | `200`; email is present |
| Email is required but missing | `GET /api/customers/{id}?emailRqd=true` | In this sample's chosen behavior, `400` if the customer has no email |
| Customer does not exist | `GET /api/customers/{unknownId}` | Typically `404`, depending on the API's design |

The `400` behavior for a missing required email is a sample design
decision, not a rule imposed by Pact. Your real API should follow its
documented requirements.

------------------------------------------------------------------------

## Optional fields, nullable fields, and matchers

Contract tests should describe what the consumer actually relies on.

These concepts are different:

-   **Required and non-null:** the property must be present and have a
    non-null value of the expected type.
-   **Required but nullable:** the property must be present, but its
    value can be a string or `null`.
-   **Optional:** the property may be absent from the response entirely.

For example, this contract body checks only these three properties:

``` csharp
.WithJsonBody(new
{
    id = Match.Integer(101),
    firstName = Match.Type("Ava"),
    lastName = Match.Type("Shah")
});
```

It does not require `email` or `state` to be present. If the provider
returns additional properties, those extra properties generally do not
cause a body mismatch by themselves.

That is different from explicitly expecting:

``` csharp
.WithJsonBody(new
{
    id = Match.Integer(101),
    firstName = Match.Type("Ava"),
    lastName = Match.Type("Shah"),
    email = (string?)null,
    state = (string?)null
});
```

Here, `email` and `state` are part of the expected body and are expected
to be null for this example.

If the consumer must handle both null and non-null values, cover both
behaviors with appropriate Pact interactions and matchers supported by
the PactNet version in use. Avoid making a contract stricter than the
consumer's actual needs.

------------------------------------------------------------------------

## Where Pact fits in the test strategy

Pact complements other test types; it does not replace them.

| Test type | What it primarily checks | Typical scope |
| --- | --- | --- |
| Unit tests | Individual functions/classes and business rules | Small, isolated components |
| Integration tests | Components working together, such as API + repository/database | A service and its dependencies |
| Consumer Pact tests | The request/response expectations of the consumer | Consumer interaction against a mock server |
| Provider Pact verification | Whether the real provider satisfies consumer contracts | Provider API against published/local Pact files |
| End-to-end tests | A broader user or business journey across multiple services | Larger integrated environment |

A practical strategy is to keep many fast unit tests, add integration
tests for important component boundaries, use Pact for
service-to-service compatibility, and reserve end-to-end tests for
critical flows that genuinely need the wider system.

------------------------------------------------------------------------

## Where Pact fits in CI/CD

Pact should run early enough to provide useful feedback and also be used
as a deployment safety check.

### A practical pipeline

``` text
Developer opens a pull request
          |
          v
Build + static checks
          |
          v
Unit tests
          |
          v
Consumer Pact tests
          |
          v
Publish Pact contract
          |
          +----------------------------+
          |                            |
          v                            v
Provider CI runs                  Consumer CI runs
provider unit/integration tests   consumer unit tests
          |                            |
          v                            |
Provider Pact verification <-----+
          |
          v
Publish verification result
          |
          v
Deployment compatibility check
          |
          v
Deploy to test/staging/production
```

The exact pipeline differs by organization. In a Pact Broker workflow,
consumers publish versioned Pact contracts, providers verify relevant
contracts, and deployment checks can use recorded verification results
to determine whether a particular application version is compatible with
the versions it will interact with.

### Recommended placement

**Consumer CI** 1. Build the consumer. 2. Run unit tests. 3. Run
consumer Pact tests. 4. Publish the generated Pact contract to a Pact
Broker (if used).

**Provider CI** 1. Build the provider. 2. Run unit and integration
tests. 3. Start the provider in a controlled test environment. 4.
Configure provider-state setup. 5. Verify the relevant consumer
contracts. 6. Publish verification results to the Pact Broker (if used).

**Before deployment** - Check compatibility for the versions that will
actually coexist. - Use the Pact Broker's deployment/release workflow
and compatibility checks where available. - Avoid deploying solely
because a Pact file exists; a consumer-generated contract is not proof
that the provider has passed verification.

**After deployment** - Continue monitoring service health and business
behavior. - Contract tests cannot detect every runtime, infrastructure,
security, performance, or business-process problem.

### Do I need a Pact Broker?

No. For a learning/reference project, a Pact JSON file can be written to
a local directory and loaded by the provider verification test.

For multiple teams and CI/CD, a **Pact Broker** (or a compatible
PactFlow setup) is useful for sharing versioned contracts, storing
verification results, and checking compatibility between application
versions. A local file is fine for learning, but coordinating contracts
manually becomes harder as the number of consumers and providers grows.

------------------------------------------------------------------------

## Best practices

1.  **Test consumer-relevant behavior.** Avoid asserting every field in
    a response if the consumer does not use it.
2.  **Keep contracts intentional.** A contract is an executable
    compatibility agreement, not a dump of the entire API response.
3.  **Use provider states for meaningful preconditions.** Give states
    clear names and implement their setup explicitly.
4.  **Make verification deterministic.** Do not depend on arbitrary data
    in a developer's local database.
5.  **Keep test data isolated.** Especially when using shared databases
    or static in-memory repositories.
6.  **Test important failure responses when the consumer relies on
    them.** For example, add a `404` interaction if the consumer needs
    to handle missing customers.
7.  **Do not confuse consumer examples with production data.** An ID
    such as `101` in a Pact request is an example used by the
    interaction. The provider must still handle that request during
    verification.
8.  **Keep provider-state setup secure.** Restrict test-only setup
    endpoints to controlled test environments; do not expose
    unrestricted data mutation endpoints in production.
9.  **Version and publish contracts consistently.** In a multi-team
    pipeline, publish contracts and verification results against
    identifiable application versions/commits.
10. **Use deployment compatibility checks.** Verification results are
    most valuable when they help prevent incompatible versions from
    being deployed together.
11. **Keep the suite focused and fast.** Pact is most effective when it
    tests the service boundaries and behaviors that matter to consumers.
12. **Confirm APIs against your installed PactNet version.** Builder
    APIs and configuration options can vary by version; use the PactNet
    documentation for the version used in the project.

------------------------------------------------------------------------

## Common misunderstandings and troubleshooting

### "My consumer Pact test passed. Does that mean the provider is compatible?"

Not by itself. The consumer test runs against the Pact mock server.
Provider verification must also pass against the actual provider.

### "Why did provider verification return `404`?"

The recorded request may target an ID that does not exist in the
provider's test data. Set up that record using a provider state or
another deterministic test-data mechanism.

### "Why did provider verification fail before calling the API?"

Check the provider-state setup logs. The state endpoint may have
returned a non-success status, rejected an unknown state name, or failed
while setting up test data. A setup-state failure is different from a
response-body mismatch.

### "Why does an interaction without `.Given()` fail?"

It can fail if the provider's current data does not satisfy the request.
If a state-change endpoint is configured and receives an empty/missing
state for that interaction, make sure that behavior is deliberately
supported---or arrange deterministic baseline data elsewhere.

### "Does `.Given()` create a database record automatically?"

No. It declares a provider state in the contract. Your provider-state
handler must perform the required setup.

### "If I omit `email` from the expected body, must the provider omit it?"

No. Omitting a property from the contract means that property is not
part of that expected-body assertion. It does not mean the provider must
omit it.

### "Can Pact replace integration or end-to-end tests?"

No. Pact is targeted at consumer/provider compatibility. Keep the other
test types for the concerns they cover.

------------------------------------------------------------------------

## Suggested repository layout

A small reference solution can be organized like this:

``` text
CustomerPactReference/
├── pacts/
│   ├── CustomerConsumer-CustomerApi.json
│   └── CustomerConsumerNew-CustomerApi.json
├── CustomerApi/
│   ├── Controllers/
│   │   ├── CustomersController.cs
│   │   └── ProviderStatesController.cs
│   ├── Models/
│   │   └── CustomerResponse.cs
│   ├── Repositories/
│   │   ├── ICustomerRepository.cs
│   │   └── CustomerRepository.cs
│   └── Program.cs
├── CustomerConsumer.Tests/
│   └── ...
├── CustomerConsumerNew.Tests/
│   └── ...
├── CustomerProvider.Tests/
│   └── ...
└── README.md
```

The exact layout can differ. Generated Pact files may be ignored by Git
for local-only experiments, or committed/published according to the
team's contract-sharing workflow. In a multi-team setup, publishing
versioned contracts to a Pact Broker is generally more scalable than
manually sharing files.

------------------------------------------------------------------------

## References

-   [Pact documentation](https://docs.pact.io/)
-   [PactNet on GitHub](https://github.com/pact-foundation/pact-net)
-   [Pact provider
    states](https://docs.pact.io/getting_started/provider_states)
-   [Pact Broker](https://docs.pact.io/pact_broker)
-   [Pact deployment and release
    checks](https://docs.pact.io/pact_broker/can_i_deploy)

------------------------------------------------------------------------

## Final takeaway

PactNet helps make service boundaries safer by turning consumer
expectations into executable contracts and verifying those contracts
against the real provider.

-   **Consumer Pact tests** define what the consumer relies on.
-   **Provider verification** checks the real provider against those
    expectations.
-   **Provider states** arrange the conditions required to verify each
    interaction.
-   **CI/CD compatibility checks** help teams avoid deploying
    incompatible versions together.

The main value is earlier, clearer feedback about service
compatibility---without requiring every team to run the entire
distributed system for every contract test.
