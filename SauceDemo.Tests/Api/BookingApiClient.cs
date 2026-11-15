using System.Text;
using System.Text.Json;

namespace SauceDemo.Tests.Api;

/// <summary>
/// Cliente fino sobre o <see cref="IAPIRequestContext"/> do Playwright. Devolve sempre a resposta crua,
/// para os testes poderem verificar status codes; <see cref="ReadAsync{T}"/> converte o corpo quando é preciso.
/// </summary>
public sealed class BookingApiClient(IAPIRequestContext request)
{
    public Task<IAPIResponse> PingAsync() => request.GetAsync("/ping");

    public Task<IAPIResponse> CreateTokenAsync(string username, string password) =>
        request.PostAsync("/auth", WithJson(new { username, password }));

    public async Task<string> GetTokenAsync(string username, string password)
    {
        var response = await CreateTokenAsync(username, password);
        using var json = JsonDocument.Parse(await response.TextAsync());
        return json.RootElement.TryGetProperty("token", out var token)
            ? token.GetString()!
            : throw new InvalidOperationException($"Sem token na resposta do /auth: {json.RootElement}");
    }

    public Task<IAPIResponse> CreateBookingAsync(object booking) =>
        request.PostAsync("/booking", WithJson(booking));

    public Task<IAPIResponse> GetBookingAsync(int id) => request.GetAsync($"/booking/{id}");

    public Task<IAPIResponse> GetBookingIdsAsync(string firstname, string lastname) =>
        request.GetAsync("/booking", new()
        {
            Params = new Dictionary<string, object> { ["firstname"] = firstname, ["lastname"] = lastname },
        });

    public Task<IAPIResponse> UpdateBookingAsync(int id, Booking booking, string? token) =>
        request.PutAsync($"/booking/{id}", WithJson(booking, token));

    public Task<IAPIResponse> PartialUpdateBookingAsync(int id, object fields, string? token) =>
        request.PatchAsync($"/booking/{id}", WithJson(fields, token));

    public Task<IAPIResponse> DeleteBookingAsync(int id, string? token) =>
        request.DeleteAsync($"/booking/{id}", new() { Headers = AuthHeaders(token) });

    /// <summary>A documentação aceita também Basic Auth, em alternativa ao cookie com o token.</summary>
    public Task<IAPIResponse> DeleteBookingWithBasicAuthAsync(int id, string username, string password) =>
        request.DeleteAsync($"/booking/{id}", new()
        {
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")),
            },
        });

    public static async Task<T> ReadAsync<T>(IAPIResponse response) =>
        JsonSerializer.Deserialize<T>(await response.TextAsync(), ApiJson.Options)
        ?? throw new InvalidOperationException($"Corpo vazio na resposta {response.Status} de {response.Url}.");

    private static APIRequestContextOptions WithJson(object body, string? token = null)
    {
        var headers = AuthHeaders(token);
        headers["Content-Type"] = "application/json";
        return new() { DataString = JsonSerializer.Serialize(body, ApiJson.Options), Headers = headers };
    }

    private static Dictionary<string, string> AuthHeaders(string? token) =>
        token is null ? [] : new() { ["Cookie"] = $"token={token}" };
}
