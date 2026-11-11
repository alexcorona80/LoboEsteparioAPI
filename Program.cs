using LoboEsteparioAPI.Data;
using LoboEsteparioAPI.Options;
using LoboEsteparioAPI.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<OvertureOptions>()
    .Bind(builder.Configuration.GetSection(OvertureOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<MongoOptions>()
    .Bind(builder.Configuration.GetSection(MongoOptions.SectionName))
    .ValidateOnStart();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IDuckDbConnectionFactory, DuckDbConnectionFactory>();
builder.Services.AddScoped<IOverturePlacesService, OverturePlacesService>();

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<MongoOptions>>().Value.DatabaseName));
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<ISentEmailRepository, SentEmailRepository>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
builder.Services.AddScoped<IAIConfigRepository, AIConfigRepository>();
builder.Services.AddHostedService<StartupInitializer>();

// Configuración para proxies inversos (Nginx, Traefik, Docker, Cloudflare)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("EnableOpenApi", true))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// En producción detrás de proxy inverso, la redirección HTTPS se maneja a nivel de proxy
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();
