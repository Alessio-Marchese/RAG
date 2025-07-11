using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Configura i parametri di validazione JWT per il middleware custom
var jwtSection = builder.Configuration.GetSection("Jwt");
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSection["Issuer"],
    ValidAudience = jwtSection["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]))
};
builder.Services.AddSingleton(tokenValidationParameters);
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<S3StorageService>();
builder.Services.AddScoped<IUserConfigService, UserConfigService>();
builder.Services.AddScoped<IS3StorageService, S3StorageService>();
builder.Services.AddSingleton<IPineconeService>(sp => sp.GetRequiredService<PineconeService>());

// Configurazione Pinecone
var pineconeApiKey = "pcsk_72Bbs7_TuyDgTjhKdGbL1EhFx8hSJctXBmpp6nxwJVtrkfdaxPu7fWsr5Qdj5zuJN3gPL4";
var pineconeIndexHost = "n8n2-n3lknmm.svc.aped-4627-b74a.pinecone.io";
builder.Services.AddHttpClient<PineconeService>((sp, client) =>
{
    // HttpClient configurato per PineconeService
});
builder.Services.AddSingleton<PineconeService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PineconeService));
    return new PineconeService(httpClient, pineconeApiKey, pineconeIndexHost);
});

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost8081",
        policy => policy.WithOrigins("http://localhost:8081")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost8081");

// Middleware custom per validazione JWT da cookie
app.UseMiddleware<CookieJwtValidationMiddleware>();

app.UseAuthorization();

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
