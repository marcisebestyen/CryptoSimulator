using CryptoSimulator.Configuration;
using CryptoSimulator.Data;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Insert services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Database context registration (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? BuildConnectionStringFromEnv(DotEnv.Load(builder.Environment.ContentRootPath));
builder.Services.AddDbContext<CryptoSimulationDbContext>(options =>
    options.UseNpgsql(connectionString));
// UnitOfWork registration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
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
});
// Repository registrations
builder.Services.AddScoped(typeof(IRepository<Crypto>), typeof(Repository<Crypto>));
builder.Services.AddScoped(typeof(IRepository<CryptoLog>), typeof(Repository<CryptoLog>));
builder.Services.AddScoped(typeof(IRepository<MyCryptos>), typeof(Repository<MyCryptos>));
builder.Services.AddScoped(typeof(IRepository<Transactions>), typeof(Repository<Transactions>));
builder.Services.AddScoped(typeof(IRepository<User>), typeof(Repository<User>));
builder.Services.AddScoped(typeof(IRepository<Wallet>), typeof(Repository<Wallet>));
// Service registrations
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<ICryptoLogService, CryptoLogService>();
builder.Services.AddScoped<IMyCryptosService, MyCryptosService>();
builder.Services.AddHostedService<PriceUpdateService>();
builder.Services.AddScoped<IPriceAlertService, PriceAlertService>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
// AutoMapper registration
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
