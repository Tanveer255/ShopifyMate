using System.Security.Cryptography;

namespace ShopifyMate.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ShopifyWebhookController(
    IConfiguration config
    ) : ControllerBase
{
    private readonly IConfiguration _config = config;
    [HttpPost("uninstall")]
    public async Task<IActionResult> HandleUninstallWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var hmacHeader = Request.Headers["X-Shopify-Hmac-Sha256"];
        if (!VerifyShopifyWebhook(body, hmacHeader))
            return Unauthorized("Invalid webhook");

        var shopifyData = JsonSerializer.Deserialize<ShopifyWebhookPayload>(body);

        // Remove shop from database
        FakeDatabase.Remove(shopifyData.ShopDomain);

        return Ok();
    }

    private bool VerifyShopifyWebhook(string requestBody, string hmacHeader)
    {
        var secret = _config["Shopify:ClientSecret"];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody)));
        return hash == hmacHeader;
    }

    public class ShopifyWebhookPayload
    {
        public string ShopDomain { get; set; }
    }

    private static Dictionary<string, string> FakeDatabase = new Dictionary<string, string>();
}
