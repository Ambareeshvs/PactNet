using CustomerApi.Repositories;
using CustomerApi.Controllers;

namespace CustomerApi;

public static class ApiApplication
{
    public static WebApplication Create(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers().AddApplicationPart(typeof(CustomersController).Assembly);
        builder.Services.AddSingleton<ICustomerRepository, CustomerRepository>();

        var app = builder.Build();
        app.MapControllers();
        return app;
    }
}
