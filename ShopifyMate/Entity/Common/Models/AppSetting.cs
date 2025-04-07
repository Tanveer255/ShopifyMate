namespace ShopifyMate.Entity.Common.Models;

public class AppSetting
{
    public Shopify Shopify { get; set; }
}
public class Shopify
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Scopes { get; set; }
    public string RedirectUri { get; set; }
}
