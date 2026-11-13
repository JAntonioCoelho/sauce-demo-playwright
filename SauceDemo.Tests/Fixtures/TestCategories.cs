namespace SauceDemo.Tests.Fixtures;

public static class TestCategories
{
    /// <summary>
    /// Casos que falham de propósito: o utilizador tem um bug conhecido e o teste tem de o apanhar.
    /// Ficam fora do gate da pipeline (<c>--filter "Category!=KnownBug"</c>) e correm num passo à parte.
    /// </summary>
    public const string KnownBug = "KnownBug";
}
