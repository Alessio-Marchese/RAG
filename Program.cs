using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Amazon.S3;
using Amazon.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RAG.Data;
using RAG.Services;
using RAG.Middlewares;
using RAG.Facades;
using RAG.Repositories;
using RAG.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppConfiguration>(builder.Configuration);
builder.Services.AddSingleton<IValidateOptions<AppConfiguration>, ConfigurationValidator>();

builder.Services.AddSingleton<TokenValidationParameters>(sp =>
{
    var config = sp.GetRequiredService<IOptions<AppConfiguration>>().Value;
    return new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = config.Jwt.Issuer,
        ValidAudience = config.Jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Jwt.Key))
    };
});

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = sp.GetRequiredService<IOptions<AppConfiguration>>().Value;
    var awsCredentials = new BasicAWSCredentials(config.AWS.AccessKey, config.AWS.SecretKey);
    var awsRegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(config.AWS.Region);
    return new AmazonS3Client(awsCredentials, awsRegionEndpoint);
});

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<IOptions<AppConfiguration>>().Value;
    var connectionString = builder.Environment.IsDevelopment() 
        ? "Data Source=rag_database.db"
        : config.ConnectionStrings.DefaultConnection;
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IUserConfigurationService, UserConfigurationService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IUsersFacade, UsersFacade>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IExceptionBoundary, ExceptionBoundary>();

builder.Services.AddScoped<IKnowledgeRuleRepository, KnowledgeRuleRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<PineconeService>();
builder.Services.AddSingleton<IPineconeService>(sp =>
{
    var config = sp.GetRequiredService<IOptions<AppConfiguration>>().Value;
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PineconeService));
    return new PineconeService(httpClient, config.Pinecone.ApiKey, config.Pinecone.IndexHost);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend",
            policy => policy.WithOrigins("https://assistsman.com")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials());
    });
}

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
    });
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
}

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseMiddleware<CookieJwtValidationMiddleware>(app.Services.GetRequiredService<TokenValidationParameters>());

app.UseMiddleware<RateLimitMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
