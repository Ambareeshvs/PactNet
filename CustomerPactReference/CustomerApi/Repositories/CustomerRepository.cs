using System.Collections.Concurrent;
using CustomerApi.Models;

namespace CustomerApi.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<int, CustomerResponse> customers = new();

    private  readonly int[] PactCustomerIds =
        [101, 102, 103, 104, 105, 106];

    public  CustomerResponse? GetById(int id)
    {
        return customers.TryGetValue(id, out var customer)
            ? customer
            : null;
    }

    public void Set(CustomerResponse customer)
    {
        customers[customer.Id] = customer;
    }

    public void ResetPactCustomers()
    {
        foreach (var id in PactCustomerIds)
        {
            customers.TryRemove(id, out _);
        }
    }
}
