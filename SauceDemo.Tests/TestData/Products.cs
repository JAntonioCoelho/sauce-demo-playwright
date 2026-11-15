namespace SauceDemo.Tests.TestData;

public static class Products
{
    public const string Backpack = "Sauce Labs Backpack";
    public const string BikeLight = "Sauce Labs Bike Light";
    public const string BoltTShirt = "Sauce Labs Bolt T-Shirt";
    public const string FleeceJacket = "Sauce Labs Fleece Jacket";
    public const string Onesie = "Sauce Labs Onesie";
    public const string RedTShirt = "Test.allTheThings() T-Shirt (Red)";

    public const int TotalInCatalog = 6;

    /// <summary>Ids internos do Sauce Demo, usados no carrinho guardado no localStorage (ver <c>CartState</c>).</summary>
    public static readonly IReadOnlyDictionary<string, int> Ids = new Dictionary<string, int>
    {
        [BikeLight] = 0,
        [BoltTShirt] = 1,
        [Onesie] = 2,
        [RedTShirt] = 3,
        [Backpack] = 4,
        [FleeceJacket] = 5,
    };
}
