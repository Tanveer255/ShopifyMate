using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ShopifyMate.Controllers;
[Route("api/[controller]")]
[ApiController]
public class ShopifyAuthController(
    IConfiguration config,
    HttpClient httpClient
    ) : ControllerBase
{
    private readonly IConfiguration _config = config;
    private readonly HttpClient _httpClient = httpClient;
    // Step 1: Redirect to Shopify for OAuth
    [HttpGet("install")]
    public IActionResult Install(string shop)
    {
        var clientId = _config["Shopify:ClientId"];
        var scopes = _config["Shopify:Scopes"];
        var redirectUri = _config["Shopify:RedirectUri"];

        var installUrl = $"https://{shop}/admin/oauth/authorize?client_id={clientId}&scope={scopes}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        return Redirect(installUrl);
    }

    // Step 2: Handle OAuth Callback from Shopify
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string code, string hmac, string shop, string state)
    {
        var clientId = _config["Shopify:ClientId"];
        var clientSecret = _config["Shopify:ClientSecret"];

        var tokenRequest = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code }
        };

        var response = await _httpClient.PostAsync($"https://{shop}/admin/oauth/access_token",
            new FormUrlEncodedContent(tokenRequest));

        if (!response.IsSuccessStatusCode)
            return BadRequest("Failed to get access token");

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<ShopifyTokenResponse>(responseBody);

        // Save access token in the database (Example: Store it in a Dictionary for now)
        FakeDatabase[shop] = tokenData.AccessToken;

        return Ok($"App installed successfully for {shop}");
    }
    public async Task RegisterUninstallWebhook(string shop, string accessToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);

        var webhookData = new
        {
            webhook = new
            {
                topic = "app/uninstalled",
                address = "https://yourapp.com/shopify/webhook/uninstall",
                format = "json"
            }
        };

        var json = JsonSerializer.Serialize(webhookData);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"https://{shop}/admin/api/2023-01/webhooks.json", content);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to register uninstall webhook");
    }

    private static Dictionary<string, string> FakeDatabase = new Dictionary<string, string>();

    public class ShopifyTokenResponse
    {
        public string AccessToken { get; set; }
    }
}