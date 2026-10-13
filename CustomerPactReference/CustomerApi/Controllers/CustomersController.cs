using CustomerApi.Models;
using CustomerApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(ICustomerRepository customers) : ControllerBase
{
    [HttpGet("{customerId:int}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CustomerResponse> GetCustomer(int customerId, [FromQuery] bool? emailRqd = null)
    {
        var customer = customers.GetById(customerId);
        if (customer is null)
        {
            return NotFound();
        }
        if (emailRqd == true && customer.Email is null)
        {
            return BadRequest(new
            {
                message = "The customer does not have an email address."
            });
        }

        return Ok(customer);
    }
}
