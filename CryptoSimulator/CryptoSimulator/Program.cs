using CryptoSimulator.Data;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Insert services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Database context registration
builder.Services.AddDbContext<CryptoSimulationDbContext>(options =>
    options.UseSqlServer("Server=localhost;Database=CryptoSimDb;TrustServerCertificate=True;User Id=sa;Password=yourStrong(&)Password"));
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
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
// AutoMapper registration
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());


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
