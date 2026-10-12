using TradingAnalyst.Business.DependencyInjection;
using TradingAnalyst.Data.DependencyInjection;
using TradingAnalyst.Api.Workers;
using TradingAnalyst.Api.Notifications;
using TradingAnalyst.Api.Scanning;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("Web", policy => policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddBusiness(builder.Configuration);
builder.Services.AddData(builder.Configuration);

builder.Services.AddHostedService<MarketDataWorker>();
builder.Services.Configure<ScannerOptions>(builder.Configuration.GetSection(ScannerOptions.SectionName));
builder.Services.AddSingleton<ScannerStateStore>();
builder.Services.AddHttpClient<IDiscordNotifier, DiscordNotifier>();
builder.Services.AddHostedService<MarketScannerWorker>();

// OpenAPI / Swagger
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Web");

app.UseAuthorization();

app.MapControllers();

app.Run();