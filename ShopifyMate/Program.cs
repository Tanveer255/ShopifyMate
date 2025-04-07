using ShopifyMate.Entity.Common.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<AppSetting>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<Shopify>(builder.Configuration.GetSection("Shopify"));
builder.Services.AddHttpClient("ShopifyClient", client =>
{
    client.BaseAddress = new Uri("https://{shop}/admin/api/");
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Make sure this comes after app.UseSwagger()
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
