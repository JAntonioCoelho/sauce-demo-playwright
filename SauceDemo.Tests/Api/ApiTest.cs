using SauceDemo.Tests.Config;

namespace SauceDemo.Tests.Api;

/// <summary>
/// Base dos testes de API: um <see cref="IAPIRequestContext"/> por teste, sem browser.
/// Tudo o que um teste cria é apagado no fim, porque a instância do Restful Booker é pública e partilhada.
/// </summary>
public abstract class ApiTest : PlaywrightTest
{
    private readonly List<int> _createdBookingIds = [];
    private IAPIRequestContext _request = null!;
    private string? _token;

    protected static ApiSettings Api => TestSettings.Current.Api;

    protected BookingApiClient Bookings { get; private set; } = null!;

    [SetUp]
    public async Task CreateRequestContextAsync()
    {
        _request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = Api.BaseUrl,
            // Sem Accept, o GET /booking/{id} não devolve JSON.
            ExtraHTTPHeaders = new Dictionary<string, string> { ["Accept"] = "application/json" },
        });
        Bookings = new BookingApiClient(_request);
    }

    [TearDown]
    public async Task DeleteCreatedBookingsAsync()
    {
        foreach (var id in _createdBookingIds)
        {
            try
            {
                await Bookings.DeleteBookingAsync(id, await GetTokenAsync());
            }
            catch (PlaywrightException)
            {
                // Limpeza de melhor esforço: uma reserva que já não existe não é um erro do teste.
            }
        }

        _createdBookingIds.Clear();
        await _request.DisposeAsync();
    }

    protected async Task<string> GetTokenAsync() =>
        _token ??= await Bookings.GetTokenAsync(Api.Username, Api.Password);

    /// <summary>
    /// Para respostas de POST /booking feitas diretamente pelo cliente: se a reserva foi criada, regista-a para ser apagada.
    /// </summary>
    protected async Task TrackIfCreatedAsync(IAPIResponse response)
    {
        if (response.Status != 200)
        {
            return;
        }

        using var json = System.Text.Json.JsonDocument.Parse(await response.TextAsync());
        if (json.RootElement.TryGetProperty("bookingid", out var id))
        {
            _createdBookingIds.Add(id.GetInt32());
        }
    }

    /// <summary>Cria uma reserva (por omissão, uma válida com nomes únicos) e regista-a para ser apagada no fim.</summary>
    protected async Task<CreatedBooking> CreateBookingAsync(Booking? booking = null)
    {
        var response = await Bookings.CreateBookingAsync(booking ?? Booking.Sample());
        Assert.That(response.Status, Is.EqualTo(200), $"POST /booking falhou: {await response.TextAsync()}");

        var created = await BookingApiClient.ReadAsync<CreatedBooking>(response);
        _createdBookingIds.Add(created.Bookingid);
        return created;
    }
}
