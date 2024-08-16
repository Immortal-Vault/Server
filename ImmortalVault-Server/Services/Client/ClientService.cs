using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace ImmortalVault_Server.Services.Client;

public interface IClientService
{
    Task<string?> GetLatestVersion(string repositoryOwner, string repositoryName);
    string BuildDownloadUrl(string version, string repositoryOwner, string repositoryName);
}

public class ClientService : IClientService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient = new();

    public ClientService(IConfiguration configuration)
    {
        this._configuration = configuration;
    }

    public string BuildDownloadUrl(string version, string repositoryOwner, string repositoryName)
    {
        return $"https://github.com/{repositoryOwner}/{repositoryName}/releases/download/{version}/Immortal.Vault.Setup.exe";
    }
    
    public async Task<string?> GetLatestVersion(string repositoryOwner, string repositoryName)
    {
        var githubToken = this._configuration["GITHUB_TOKEN"];
        if (githubToken is null)
        {
            throw new Exception("Github Token is missing");
        }

        var url = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/releases/latest";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("token", githubToken);
            request.Headers.UserAgent.ParseAdd("ImmortalVault"); // GitHub требует наличие заголовка User-Agent

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Network response was not ok: {response.ReasonPhrase}");
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(responseBody);
            var tagName = data["tag_name"]?.Value<string>() ?? throw new Exception("Tag name is null");
            
            return tagName;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching the latest release: {ex.Message}");
            return null;
        }
    }
}