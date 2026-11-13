namespace SauceDemo.Tests.TestData;

public sealed record CustomerInfo(string FirstName, string LastName, string PostalCode)
{
    public static CustomerInfo Valid() => new("Joana", "Silva", "1000-001");

    public CustomerInfo WithoutPostalCode() => this with { PostalCode = string.Empty };
}
