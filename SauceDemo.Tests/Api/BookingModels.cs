using System.Text.Json;
using System.Text.Json.Serialization;

namespace SauceDemo.Tests.Api;

public sealed record BookingDates(string Checkin, string Checkout);

public sealed record Booking(
    string Firstname,
    string Lastname,
    int Totalprice,
    bool Depositpaid,
    BookingDates Bookingdates,
    string? Additionalneeds = null)
{
    /// <summary>
    /// Reserva válida com nomes únicos. A instância do Restful Booker é pública e partilhada:
    /// sem nomes únicos, os filtros por nome apanhariam reservas de outras pessoas.
    /// </summary>
    public static Booking Sample()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new($"Coelho{suffix}", $"Teste{suffix}", 150, true, new("2026-12-01", "2026-12-05"), "Breakfast");
    }
}

public sealed record CreatedBooking(int Bookingid, Booking Booking);

public sealed record BookingReference(int Bookingid);

public static class ApiJson
{
    /// <summary>A API usa nomes em minúsculas ("firstname", "bookingdates"): camelCase sobre os nomes C# dá exatamente isso.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
