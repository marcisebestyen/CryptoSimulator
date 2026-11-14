using CryptoSimulator.Configuration;
using CryptoSimulator.Data;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Insert services to the container.

builder.Services.AddControllers();
// Database context registration (PostgreSQL)
var env = DotEnv.Load(builder.Environment.ContentRootPath);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? BuildConnectionStringFromEnv(env);
builder.Services.AddDbContext<CryptoSimulationDbContext>(options =>
    options.UseNpgsql(connectionString));
// UnitOfWork registration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// JWT authentication
var jwtSettings = JwtSettings.Load(builder.Configuration, env);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        };
    });
// CORS: only the configured frontend origins may call the API from a browser
var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray();
if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins is missing. Set it in appsettings or via Cors__AllowedOrigins__0.");
}
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .WithHeaders("Authorization", "Content-Type")
        .AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
// Swagger registration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "CryptoSimulator API",
        Description = "A simple API to simulate a cryptocurrency exchange",
        Contact = new OpenApiContact
        {
            Name = "Marcell Sebestyen",
            Email = "sebestyenmarcell2@gmail.com",
        }
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token returned by POST /api/users/login.",
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
    });
});
// Service registrations
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddSingleton<IRandomSource, SystemRandomSource>();
builder.Services.AddSingleton<PriceSimulator>();
builder.Services.AddHostedService<PriceUpdateService>();
builder.Services.AddScoped<IPriceAlertService, PriceAlertService>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
// AutoMapper registration
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string BuildConnectionStringFromEnv(Dictionary<string, string> env)
{
    string Get(string key) => env.TryGetValue(key, out var value) && value.Length > 0
        ? value
        : throw new InvalidOperationException(
            $"{key} is missing. Create a .env file (see .env.example) or set ConnectionStrings__DefaultConnection.");

    return new NpgsqlConnectionStringBuilder
    {
        Host = env.GetValueOrDefault("POSTGRES_HOST", "localhost"),
        Port = int.Parse(env.GetValueOrDefault("POSTGRES_PORT", "5432")),
        Database = Get("POSTGRES_DB"),
        Username = Get("POSTGRES_USER"),
        Password = Get("POSTGRES_PASSWORD"),
    }.ConnectionString;
}

public partial class Program { }
