namespace CustomerApi.Models;

public sealed record CustomerResponse(int Id,
    string FirstName,
    string LastName,
    string? Email,
    string? State);
