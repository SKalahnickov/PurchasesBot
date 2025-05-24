// See https://aka.ms/new-console-template for more information

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

Console.WriteLine("Hello, World!");
// PurchasesManagerBrasilBot
var botClient = new TelegramBotClient("7500348707:AAElWCtrzYnQ8mxs5MaRtBnasELrp8RLjXo");

// Хранилище состояний пользователей
var userStates = new ConcurrentDictionary<long, UserFormState>();

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>() // получать все типы апдейтов
};

// Объявление методов до их использования
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Универсально ищем сообщение в любом поле апдейта
    var message = update.Message ?? update.EditedMessage ?? update.ChannelPost ?? update.EditedChannelPost;
    if (message is null)
        return;
    var chatId = message.Chat.Id;
    var text = message.Text;

    var state = userStates.GetOrAdd(chatId, _ => new UserFormState());

    // Глобальная обработка фото вне зависимости от шага
    if (message.Photo != null && message.Photo.Length > 0 && !string.IsNullOrEmpty(message.MediaGroupId))
    {
        if (message.MediaGroupId == state.MediaGroupId)
        {
            var fileId = message.Photo[^1].FileId;
            if (!state.PhotoFileIds.Contains(fileId))
                state.PhotoFileIds.Add(fileId);
        }
    }

    // Обработка команды /start
    if (text == "/start")
    {
        userStates[chatId] = new UserFormState();
        await botClient.SendMessage(
            chatId: chatId,
            text: "Привет! Давайте заполним форму. Введите название товара:",
            cancellationToken: cancellationToken
        );
        return;
    }

    switch (state.Step)
    {
        case 0:
            if (!string.IsNullOrWhiteSpace(message.Text))
            {
                state.Name = message.Text;
                state.Step = 1;
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Отправьте фото товара:",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Пожалуйста, введите название товара:",
                    cancellationToken: cancellationToken
                );
            }
            break;
        case 1:
            if (message.Photo != null && message.Photo.Length > 0)
            {
                state.MediaGroupId = message.MediaGroupId;
                state.PhotoFileIds ??= new HashSet<string>();
                state.PhotoFileIds.Add(message.Photo[^1].FileId);
                state.Step = 2;
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Введите цену товара:",
                    cancellationToken: cancellationToken
                );
            }
            break;
        case 2:
            if (!string.IsNullOrWhiteSpace(message.Text))
            {
                // Обработка цены с валютой
                var priceInput = message.Text.Trim();
                string formattedPrice = priceInput;
                var priceMatch = System.Text.RegularExpressions.Regex.Match(priceInput, @"^(\d+[.,]?\d*)\s*(.*)$");
                if (priceMatch.Success)
                {
                    var number = priceMatch.Groups[1].Value.Replace(',', '.');
                    var currency = priceMatch.Groups[2].Value.Trim().ToLower();
                    if (string.IsNullOrEmpty(currency) || currency == "реал" || currency == "реалов")
                    {
                        formattedPrice = $"{number} R$";
                    }
                    else
                    {
                        formattedPrice = priceInput;
                    }
                }
                state.Price = formattedPrice;
                state.Step = 3;
                // Кнопки для выбора раздела
                var sectionKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("🥦 Овощи и фрукты"), new KeyboardButton("🍖 Мясо и рыба") },
                    new[] { new KeyboardButton("🧀 Молочка и сыры"), new KeyboardButton("🥗 Готовое и вкусное") },
                    new[] { new KeyboardButton("🥫 Долгого хранения"), new KeyboardButton("🧃 Напитки") },
                    new[] { new KeyboardButton("🤔 Странное и интересное") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Выберите раздел:",
                    replyMarkup: sectionKeyboard,
                    cancellationToken: cancellationToken
                );
            }
            break;
        case 3:
            var validSections = new[]
            {
                "🥦 Овощи и фрукты",
                "🍖 Мясо и рыба",
                "🧀 Молочка и сыры",
                "🥗 Готовое и вкусное",
                "🥫 Долгого хранения",
                "🧃 Напитки",
                "🤔 Странное и интересное"
            };
            if (validSections.Contains(message.Text?.Trim()))
            {
                state.Section = message.Text.Trim();
                state.Step = 4;
                // Кнопки для оценки
                var ratingKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("Прекрасно"), new KeyboardButton("Ужасно") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Оцените товар:",
                    replyMarkup: ratingKeyboard,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Пожалуйста, выберите раздел из списка:",
                    cancellationToken: cancellationToken
                );
            }
            break;
        case 4:
            if (message.Text is "Прекрасно" or "Ужасно")
            {
                state.Rating = message.Text;
                state.Step = 5;
                // Кнопки для выбора: добавить комментарий или пропустить
                var commentKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("Добавить комментарий"), new KeyboardButton("Пропустить") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Хотите добавить комментарий?",
                    replyMarkup: commentKeyboard,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Пожалуйста, выберите: Прекрасно или Ужасно.",
                    cancellationToken: cancellationToken
                );
            }
            break;
        case 5:
            if (message.Text == "Пропустить")
            {
                // Пропуск комментария
                goto case 6;
            }
            else if (message.Text == "Добавить комментарий")
            {
                state.Step = 6;
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Введите комментарий:",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                break;
            }
            else
            {
                // Если пользователь сразу ввёл текст, считаем это комментарием
                state.Comment = message.Text;
                goto case 6;
            }
        case 6:
            if (state.Step == 6 && !string.IsNullOrWhiteSpace(message.Text) && message.Text != "Добавить комментарий")
                state.Comment = message.Text;
            // Формируем структурированный вывод с учётом комментария и тегов
            var resultMsg = $"🛒 <b>Новая находка</b>\n" +
                            $"🍗 <b>Название:</b> <i>{state.Name}</i>\n" +
                            $"💰 <b>Цена:</b> <i>{state.Price}</i>\n" +
                            $"🏷 <b>Раздел:</b> <i>{state.Section}</i>\n" +
                            $"⭐️ <b>Оценка:</b> <i>{(state.Rating == "Прекрасно" ? "🤤 прекрасно" : "🤢 ужасно")}</i>";
            if (!string.IsNullOrWhiteSpace(state.Comment))
            {
                resultMsg += $"\n📝 <b>Комментарий:</b> <i>{state.Comment}</i>";
            }
            // Тэги для поиска
            // Для хэштега раздела и названия берём только буквы и приводим к нижнему регистру
            var sectionTag = new string((state.Section ?? "").Where(char.IsLetter).ToArray()).ToLower();
            var nameTag = string.Join("_", (state.Name ?? "").Split(' ').Select(w => new string(w.Where(char.IsLetter).ToArray()).ToLower()).Where(s => !string.IsNullOrWhiteSpace(s)));
            var tags = $"\n\n#находка #{sectionTag} #оценка_{(state.Rating == "Прекрасно" ? "прекрасно" : "ужасно")}";
            if (!string.IsNullOrWhiteSpace(nameTag))
                tags += $" #{nameTag}";
            resultMsg += tags;
            await botClient.SendMessage(
                chatId: chatId,
                text: resultMsg,
                replyMarkup: new ReplyKeyboardRemove(),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken
            );
            await botClient.SendMediaGroup(
                chatId: chatId,
                state.PhotoFileIds.Select(x => new InputMediaPhoto(x)).ToArray(),
                cancellationToken: cancellationToken
            );
            userStates.TryRemove(chatId, out _);
            break;
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken __)
{
    Console.WriteLine($"Ошибка: {exception.Message}");
    return Task.CompletedTask;
}

botClient.StartReceiving(
    HandleUpdateAsync,
    HandlePollingErrorAsync,
    receiverOptions,
    cts.Token
);

Console.WriteLine("Бот запущен. Нажмите Enter для выхода.");
Console.ReadLine();

cts.Cancel();

// Структура для хранения состояния пользователя
class UserFormState
{
    public string? Name { get; set; }
    public HashSet<string> PhotoFileIds { get; set; } = new HashSet<string>();
    public string? MediaGroupId { get; set; }
    public bool AlbumMediaGroupIdSet { get; set; } = false;
    public string? Price { get; set; }
    public string? Section { get; set; }
    public string? Rating { get; set; }
    public string? Comment { get; set; }
    public int Step { get; set; } = 0;
}

