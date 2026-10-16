using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
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

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<ILinkService, LinkService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UrlShortener API",
        Version = "v1",
        Description = "API de encurtador de URL desenvolvida em .NET 10 com Minimal APIs, SQLite e EF Core."
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Chave de acesso para estatísticas (ex: secret-stats-key). Informe no header 'X-Api-Key'.",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey", null, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UrlShortener API v1");
    });
}

app.MapGet("/", () => "UrlShortener API")
    .WithTags("Status")
    .WithSummary("Healthcheck e status da API");

app.MapLinkEndpoints();

app.Run();

public partial class Program { }
