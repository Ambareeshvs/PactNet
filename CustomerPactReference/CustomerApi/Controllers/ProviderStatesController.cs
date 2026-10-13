using CustomerApi.Models;
using CustomerApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers;

[ApiController]
[Route("provider-states")]
public sealed class ProviderStatesController(ICustomerRepository customers) : ControllerBase
{
    [HttpPost]
    public IActionResult SetState([FromBody] ProviderStateRequest request)
    {
        customers.ResetPactCustomers();

        var customer = request.State switch
        {
            "Customer has null email and null state" =>
                new CustomerResponse(
                    101, "Ava", "Shah", null, null),

            "Customer has email and approved state" =>
                new CustomerResponse(
                    102, "Noah", "Patel",
                    "noah@example.com", "Approved"),

            "Customer has required email and null state" =>
                new CustomerResponse(
                    103, "Mia", "Jones",
                    "mia@example.com", null),

            "Customer has required email and approved state" =>
                new CustomerResponse(
                    104, "Liam", "Brown",
                    "liam@example.com", "Approved"),

            "Customer has optional null email and approved state" =>
                new CustomerResponse(
                    105, "Zoe", "Wilson", null, "Approved"),

            "Customer has optional email and null state" =>
                new CustomerResponse(
                    106, "Ethan", "Davis",
                    "ethan@example.com", null),

            _ => null
        };

        if (customer is null)
        {
            return BadRequest(
                $"Unknown provider state: {request.State}");
        }

        customers.Set(customer);

        return Ok();
    }
}

public sealed record ProviderStateRequest(string State);
