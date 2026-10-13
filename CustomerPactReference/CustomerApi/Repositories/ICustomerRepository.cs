using CustomerApi.Models;

namespace CustomerApi.Repositories;

public interface ICustomerRepository
{
    CustomerResponse? GetById(int id);

    void Set(CustomerResponse customer);

    void ResetPactCustomers();
}
