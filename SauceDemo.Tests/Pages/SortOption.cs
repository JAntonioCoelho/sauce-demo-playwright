namespace SauceDemo.Tests.Pages;

public enum SortOption
{
    NameAscending,
    NameDescending,
    PriceLowToHigh,
    PriceHighToLow,
}

internal static class SortOptionExtensions
{
    public static string ToOptionValue(this SortOption option) => option switch
    {
        SortOption.NameAscending => "az",
        SortOption.NameDescending => "za",
        SortOption.PriceLowToHigh => "lohi",
        SortOption.PriceHighToLow => "hilo",
        _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
    };
}
