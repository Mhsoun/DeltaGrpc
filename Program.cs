using CurrencyDeltaGrpc.Models;
using CurrencyDeltaGrpc.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.Configure<ExchangeRateApiSettings>(builder.Configuration.GetSection("ExchangeRateApi"));
builder.Services.AddHttpClient();

var app = builder.Build();

// Get the logger factory from the app
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapGrpcReflectionService();
}

app.UseHttpsRedirection();
//app.UseAuthorization();

app.MapGrpcService<CurrencyDeltaService>();
app.MapGrpcReflectionService();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
