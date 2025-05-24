using PurchasesBot;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddSingleton<PurchasesBotService>();
builder.Services.AddScoped<IPurchasesBotService, PurchasesBotService>();

var app = builder.Build();

// Получаем токен из конфигурации
var config = app.Services.GetRequiredService<IConfiguration>();
var botToken = config["BotConfiguration:TelegramToken"] ?? Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
if (string.IsNullOrWhiteSpace(botToken))
{
    app.Logger.LogError("Не задан токен Telegram-бота в appsettings.json или переменных окружения.");
    return;
}

// Запускаем сервис бота
var botService = app.Services.GetRequiredService<IPurchasesBotService>();
botService.Start();

app.MapGet("/", () => "PurchasesBot is running!");

app.Lifetime.ApplicationStopping.Register(() => botService.Stop());

app.Run();
