using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using RAG.Data;
using RAG.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// In produzione, la porta viene gestita da ASPNETCORE_URLS o appsettings.Production.json

// Configura i parametri di validazione JWT per il middleware custom
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSection["Issuer"],
    ValidAudience = jwtSection["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
};
builder.Services.AddSingleton(tokenValidationParameters);

// Servizi AWS S3
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<S3StorageService>();

// Configurazione database SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                     "Data Source=rag_database.db"));

// Servizi applicazione
builder.Services.AddScoped<IUserConfigService, UserConfigService>();
builder.Services.AddScoped<IS3StorageService, S3StorageService>();
builder.Services.AddScoped<SqliteDataService>();

// Configurazione Pinecone da appsettings.json
var pineconeSection = builder.Configuration.GetSection("Pinecone");
var pineconeApiKey = pineconeSection["ApiKey"] ?? throw new InvalidOperationException("Pinecone ApiKey is not configured");
var pineconeIndexHost = pineconeSection["IndexHost"] ?? throw new InvalidOperationException("Pinecone IndexHost is not configured");
builder.Services.AddHttpClient<PineconeService>();
builder.Services.AddSingleton<PineconeService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PineconeService));
    return new PineconeService(httpClient, pineconeApiKey, pineconeIndexHost);
});

// Configurazione OpenAPI per .NET 8.0
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurazione CORS dinamica in base all'ambiente
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend",
            policy => policy.WithOrigins("http://localhost:8081")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials());
    });
}
else // Production o altri ambienti
{
    // Recupera l'host corrente da configurazione o variabile d'ambiente
    var allowedOrigin = builder.Configuration["AllowedCorsOrigin"] ?? "https://api.assistsman.com";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend",
            policy => policy.WithOrigins(allowedOrigin)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials());
    });
}

// Configurazione Kestrel per produzione
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
    });
    // Forza l'uso della porta 5000 per evitare conflitti
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
}

builder.Services.AddControllers();

var app = builder.Build();

// Inizializzazione database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

// Applica la policy CORS globale (sempre, anche in produzione)
app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
    // Applica la policy CORS globale (deve essere prima di qualsiasi middleware custom)
    // app.UseCors("AllowAll"); // This line is now redundant as it's moved outside
}

// Middleware custom per validazione JWT da cookie
app.UseMiddleware<CookieJwtValidationMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
