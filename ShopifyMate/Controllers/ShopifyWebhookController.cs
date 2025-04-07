using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ShopifyMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopifyWebhookController : ControllerBase
    {
        private readonly IConfiguration _config;

        // Constructor to inject IConfiguration
        public ShopifyWebhookController(IConfiguration config)
        {
            _config = config;
        }

        // Handle Shopify uninstall webhook via POST method
        [HttpPost("uninstall")]
        public async Task<IActionResult> HandleUninstallWebhook()
        {
            // Read the incoming webhook body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // Get the HMAC header to verify the request
            var hmacHeader = Request.Headers["X-Shopify-Hmac-Sha256"];
            if (!VerifyShopifyWebhook(body, hmacHeader))
                return Unauthorized("Invalid webhook");

            // Deserialize the webhook payload
            var shopifyData = JsonSerializer.Deserialize<ShopifyWebhookPayload>(body);

            // Remove shop from the "database" (in this case, FakeDatabase)
            FakeDatabase.Remove(shopifyData.ShopDomain);

            return Ok();
        }

        // Method to verify the webhook using HMAC
        private bool VerifyShopifyWebhook(string requestBody, string hmacHeader)
        {
            var secret = _config["Shopify:ClientSecret"];
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody)));
            return hash == hmacHeader;
        }

        // Payload class representing the structure of the webhook data
        public class ShopifyWebhookPayload
        {
            public string ShopDomain { get; set; }
        }

        // Fake database to simulate storing the Shopify store data
        private static Dictionary<string, string> FakeDatabase = new Dictionary<string, string>();
    }
}
