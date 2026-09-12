using LoboEsteparioAPI.Data;
using LoboEsteparioAPI.Options;
using LoboEsteparioAPI.Services;
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

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IDuckDbConnectionFactory, DuckDbConnectionFactory>();
builder.Services.AddScoped<IOverturePlacesService, OverturePlacesService>();

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<MongoOptions>>().Value.DatabaseName));
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
