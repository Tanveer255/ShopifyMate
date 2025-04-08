namespace ShopifyMate.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ShopifyAuthController(IOptions<AppSetting> appSettings, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly Shopify _shopify = appSettings.Value.Shopify;

    [HttpGet("install")]
    public IActionResult Install(string shop)
    {
        var clientId = _shopify.ClientId;
        var scopes = _shopify.Scopes;
        var redirectUri = _shopify.RedirectUri;

        var installUrl = $"https://{shop}/admin/oauth/authorize?client_id={clientId}&scope={scopes}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        return Redirect(installUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string code, string hmac, string shop, string state)
    {
        var clientId = _shopify.ClientId;
        var clientSecret = _shopify.ClientSecret;

        var tokenRequest = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code }
        };

        using var client = httpClientFactory.CreateClient();
        var response = await client.PostAsync($"https://{shop}/admin/oauth/access_token",
            new FormUrlEncodedContent(tokenRequest));

        if (!response.IsSuccessStatusCode)
            return BadRequest("Failed to get access token");

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<ShopifyTokenResponse>(responseBody);

        FakeDatabase[shop] = tokenData.AccessToken;

        await RegisterUninstallWebhook(shop, tokenData.AccessToken);

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

    private static Dictionary<string, string> FakeDatabase = new();

    public class ShopifyTokenResponse
    {
        public string AccessToken { get; set; }
    }
}
