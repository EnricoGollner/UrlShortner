using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Endpoints;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<ICodeGenerator, CodeGenerator>();
builder.Services.AddHttpClient<IUrlValidator, UrlValidator>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<ILinkService, LinkService>();

var app = builder.Build();

app.MapGet("/", () => "UrlShortener API");
app.MapLinkEndpoints();

app.Run();

public partial class Program { }
