using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeautyStudio.Services;

public sealed record UpdateCheckResult(string CurrentVersion, string? LatestVersion, string? ReleaseUrl, bool UpdateAvailable, string Message);

public sealed class UpdateCheckService
{
    private static readonly HttpClient Client = CreateClient();
    private const string ReleasesUrl = "https://api.github.com/repos/llambda/beautyShop/releases/latest";

    public async Task<UpdateCheckResult> CheckAsync()
    {
        var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        try
        {
            using var response = await Client.GetAsync(ReleasesUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new(current.ToString(3), null, null, false, "Nessuna versione stabile è stata ancora pubblicata.");
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);
            var tag = json.RootElement.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() : null;
            var url = json.RootElement.TryGetProperty("html_url", out var urlElement) ? urlElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(tag) || !Version.TryParse(tag.TrimStart('v', 'V'), out var latest))
                return new(current.ToString(3), tag, url, false, "La versione pubblicata non ha un numero versione riconoscibile.");

            bool available = latest > current;
            return new(current.ToString(3), latest.ToString(3), url, available,
                available ? $"È disponibile la versione {latest}." : "BeautyStudio è aggiornato.");
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Aggiornamenti", ex);
            return new(current.ToString(3), null, null, false, "Controllo aggiornamenti non riuscito: " + ex.Message);
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BeautyStudio", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}
