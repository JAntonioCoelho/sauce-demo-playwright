using System.Text.Json;
using SauceDemo.Tests.Fixtures;

namespace SauceDemo.Tests.Api;

/// <summary>
/// Testes de API ao Restful Booker (CRUD de reservas + autenticação), com o APIRequestContext do Playwright.
/// Os testes do gate verificam o contrato funcional. Os testes KnownBug descrevem o comportamento REST correto
/// em pontos onde o Restful Booker se desvia de propósito: têm de falhar, e avisam quando deixarem de falhar.
/// </summary>
[TestFixture]
[Category("Api")]
public sealed class BookingApiTests : ApiTest
{
    [Test]
    public async Task Auth_WithValidCredentials_ReturnsToken()
    {
        var response = await Bookings.CreateTokenAsync(Api.Username, Api.Password);

        Assert.That(response.Status, Is.EqualTo(200));
        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.That(json.RootElement.GetProperty("token").GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task Auth_WithInvalidCredentials_ReturnsNoToken()
    {
        var response = await Bookings.CreateTokenAsync(Api.Username, "password-errada");

        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.That(json.RootElement.TryGetProperty("token", out _), Is.False, "Credenciais erradas nunca podem dar um token.");
    }

    [Test]
    public async Task CreateBooking_WithValidData_ReturnsIdAndSameData()
    {
        var booking = Booking.Sample();

        var created = await CreateBookingAsync(booking);

        Assert.Multiple(() =>
        {
            Assert.That(created.Bookingid, Is.GreaterThan(0));
            Assert.That(created.Booking, Is.EqualTo(booking));
        });
    }

    [Test]
    public async Task GetBooking_AfterCreate_ReturnsSameData()
    {
        var created = await CreateBookingAsync();

        var response = await Bookings.GetBookingAsync(created.Bookingid);

        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await BookingApiClient.ReadAsync<Booking>(response), Is.EqualTo(created.Booking));
    }

    [Test]
    public async Task GetBookingIds_FilteredByName_ReturnsOnlyCreatedBooking()
    {
        var created = await CreateBookingAsync();

        var response = await Bookings.GetBookingIdsAsync(created.Booking.Firstname, created.Booking.Lastname);

        Assert.That(response.Status, Is.EqualTo(200));
        var ids = await BookingApiClient.ReadAsync<BookingReference[]>(response);
        Assert.That(ids.Select(reference => reference.Bookingid), Is.EquivalentTo(new[] { created.Bookingid }));
    }

    [Test]
    public async Task GetBooking_ThatDoesNotExist_ReturnsNotFound()
    {
        var response = await Bookings.GetBookingAsync(int.MaxValue);

        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task UpdateBooking_WithToken_ReplacesAllFields()
    {
        var created = await CreateBookingAsync();
        var replacement = Booking.Sample() with { Totalprice = 320, Depositpaid = false, Additionalneeds = "Late checkout" };

        var response = await Bookings.UpdateBookingAsync(created.Bookingid, replacement, await GetTokenAsync());

        Assert.That(response.Status, Is.EqualTo(200), await response.TextAsync());
        var stored = await BookingApiClient.ReadAsync<Booking>(await Bookings.GetBookingAsync(created.Bookingid));
        Assert.That(stored, Is.EqualTo(replacement));
    }

    [Test]
    public async Task PartialUpdateBooking_WithToken_ChangesOnlyGivenFields()
    {
        var created = await CreateBookingAsync();

        var response = await Bookings.PartialUpdateBookingAsync(
            created.Bookingid, new { totalprice = 999, additionalneeds = "Dinner" }, await GetTokenAsync());

        Assert.That(response.Status, Is.EqualTo(200), await response.TextAsync());
        var stored = await BookingApiClient.ReadAsync<Booking>(await Bookings.GetBookingAsync(created.Bookingid));
        Assert.That(stored, Is.EqualTo(created.Booking with { Totalprice = 999, Additionalneeds = "Dinner" }));
    }

    [Test]
    public async Task UpdateBooking_WithoutToken_IsForbiddenAndChangesNothing()
    {
        var created = await CreateBookingAsync();

        var response = await Bookings.UpdateBookingAsync(created.Bookingid, Booking.Sample(), token: null);

        Assert.That(response.Status, Is.EqualTo(403));
        var stored = await BookingApiClient.ReadAsync<Booking>(await Bookings.GetBookingAsync(created.Bookingid));
        Assert.That(stored, Is.EqualTo(created.Booking));
    }

    [Test]
    public async Task DeleteBooking_WithToken_RemovesBooking()
    {
        var created = await CreateBookingAsync();

        var response = await Bookings.DeleteBookingAsync(created.Bookingid, await GetTokenAsync());

        Assert.That(response.Ok, Is.True, $"DELETE devolveu {response.Status}");
        Assert.That((await Bookings.GetBookingAsync(created.Bookingid)).Status, Is.EqualTo(404));
    }

    // ---- Desvios conhecidos de REST: descrevem o comportamento correto e têm de falhar enquanto o bug existir ----

    [Test]
    [Category(TestCategories.KnownBug)]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await Bookings.PingAsync();

        Assert.That(response.Status, Is.EqualTo(200), "Um health check bem-sucedido devia devolver 200 OK.");
    }

    [Test]
    [Category(TestCategories.KnownBug)]
    public async Task Auth_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await Bookings.CreateTokenAsync(Api.Username, "password-errada");

        Assert.That(response.Status, Is.EqualTo(401), $"Corpo: {await response.TextAsync()}");
    }

    [Test]
    [Category(TestCategories.KnownBug)]
    public async Task DeleteBooking_WithToken_ReturnsOkOrNoContent()
    {
        var created = await CreateBookingAsync();

        var response = await Bookings.DeleteBookingAsync(created.Bookingid, await GetTokenAsync());

        Assert.That(response.Status, Is.EqualTo(200).Or.EqualTo(204), "Um DELETE bem-sucedido devia devolver 200 ou 204.");
    }

    [Test]
    [Category(TestCategories.KnownBug)]
    public async Task DeleteBooking_ThatDoesNotExist_ReturnsNotFound()
    {
        var response = await Bookings.DeleteBookingAsync(int.MaxValue, await GetTokenAsync());

        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test]
    [Category(TestCategories.KnownBug)]
    public async Task CreateBooking_WithMissingFields_ReturnsBadRequest()
    {
        var response = await Bookings.CreateBookingAsync(new { firstname = "SoNome" });
        await TrackIfCreatedAsync(response);

        Assert.That(response.Status, Is.EqualTo(400), $"Corpo: {await response.TextAsync()}");
    }

    [Test]
    [Category(TestCategories.KnownBug)]
    public async Task CreateBooking_WithCheckoutBeforeCheckin_IsRejected()
    {
        var booking = Booking.Sample() with { Bookingdates = new("2026-12-10", "2026-12-01") };

        var response = await Bookings.CreateBookingAsync(booking);
        await TrackIfCreatedAsync(response);

        Assert.That(response.Status, Is.EqualTo(400), $"Corpo: {await response.TextAsync()}");
    }

    [Test]
    [Category(TestCategories.KnownBug)]
    public async Task CreateBooking_WithDecimalPrice_KeepsPrecision()
    {
        var sample = Booking.Sample();
        var response = await Bookings.CreateBookingAsync(new
        {
            sample.Firstname,
            sample.Lastname,
            totalprice = 150.75m,
            sample.Depositpaid,
            sample.Bookingdates,
        });
        await TrackIfCreatedAsync(response);

        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.That(json.RootElement.GetProperty("booking").GetProperty("totalprice").GetDecimal(), Is.EqualTo(150.75m));
    }

    [Test]
    [Category(TestCategories.KnownBug)]
    public async Task DeleteBooking_WithBasicAuth_RemovesBooking()
    {
        var created = await CreateBookingAsync();

        var response = await Bookings.DeleteBookingWithBasicAuthAsync(created.Bookingid, Api.Username, Api.Password);

        Assert.That(response.Ok, Is.True, $"DELETE com Basic Auth devolveu {response.Status}");
    }
}
