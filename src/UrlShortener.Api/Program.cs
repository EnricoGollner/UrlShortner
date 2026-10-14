var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "UrlShortener API");

app.Run();

public partial class Program { }
