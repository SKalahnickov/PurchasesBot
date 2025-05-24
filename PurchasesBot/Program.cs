using PurchasesBot;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем PurchasesBotService как scoped сервис IPurchasesBotService
builder.Services.AddScoped<IPurchasesBotService, PurchasesBotService>();

// Регистрируем TokenHolder как singleton
builder.Services.AddSingleton<ITokenHolder, TokenHolder>();

var app = builder.Build();

// Получаем токен из конфигурации
var config = app.Services.GetRequiredService<IConfiguration>();
var botToken = config["BotConfiguration:TelegramToken"] ?? Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
if (string.IsNullOrWhiteSpace(botToken))
{
    app.Logger.LogError("Не задан токен Telegram-бота в appsettings.json или переменных окружения.");
    return;
}

// Заполняем TokenHolder
var tokenHolder = app.Services.GetRequiredService<ITokenHolder>();
tokenHolder.Token = botToken;

// Запускаем сервис бота
var botService = app.Services.GetRequiredService<IPurchasesBotService>();
botService?.Start();

app.MapGet("/", () => "PurchasesBot is running!");

app.Lifetime.ApplicationStopping.Register(() => botService?.Stop());

app.Run();
