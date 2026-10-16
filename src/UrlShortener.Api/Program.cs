using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<ICodeGenerator, CodeGenerator>();

var app = builder.Build();

app.MapGet("/", () => "UrlShortener API");

app.Run();

public partial class Program { }
