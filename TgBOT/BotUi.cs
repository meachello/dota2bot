namespace TgBOT;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;using Telegram.Bot.Types.ReplyMarkups;

public class BotUi
{
    TelegramBotClient botClient = new TelegramBotClient("6223991296:AAEMU4MfOBZjjVnuTHXGjVYjeT2vTEu_0jw"); 
    Commands command = new Commands();  
CancellationToken cancellationToken = new CancellationToken();
ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };


public async Task Start()
{
    botClient.StartReceiving(HandlerUpdateAsync, HandlerErrorAsync, receiverOptions, cancellationToken);
    var botMe = await botClient.GetMeAsync();
    Console.WriteLine($"Бот {botMe.Username} почав працювати");
    Console.ReadKey();
}

private async Task HandlerErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException => $"Помилка в телеграм бот АПІ:\n {apiRequestException.ErrorCode}" +
                                                   $"\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage);
    if (!cancellationToken.IsCancellationRequested)
    {
        try
        {

            await Task.Delay(1000, cancellationToken);

            botClient.StartReceiving(HandlerUpdateAsync, HandlerErrorAsync, receiverOptions, cancellationToken);
        }
        catch (Exception restartException)
        {
            Console.WriteLine($"An error occurred while restarting the bot: {restartException.Message}");
        }
    }
}

private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;
    Console.WriteLine($"Received a '{messageText}' message from {update.Message.From.Username}.");
    
    switch (command.currentStage)
    {
        case "menu":
            break;
        case "start":
            if (messageText.Contains("https://steamcommunity.com/profiles/"))
            {
                var i = messageText.Split('/');
                long id;
                if (i[^1] == "")
                {
                    id = Convert.ToInt64(i[^2]);
                }
                else
                    id = Convert.ToInt64(i[^1]);
                var request = await new HttpClient().GetAsync($"https://dotabotapi.azurewebsites.net/id64/{id}");
                id <<= 32;
                id >>= 32;
                await new HttpClient().PostAsync("https://dotabotapi.azurewebsites.net/add_id", new StringContent(System.Text.Json.JsonSerializer.Serialize(new {DotaID = id, Username = update.Message.From.Username}), Encoding.Unicode, "application/json"));
                var response = await request.Content.ReadAsStringAsync();
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: response,
                    cancellationToken: cancellationToken);
            }
            else if (new Regex(@"\d+").IsMatch(messageText))
            {
                long id = Convert.ToInt64(messageText);
                await new HttpClient().PostAsync("https://dotabotapi.azurewebsites.net/add_id", new StringContent(System.Text.Json.JsonSerializer.Serialize(new {DotaID = id, Username = update.Message.From.Username}), Encoding.Unicode, "application/json"));
                var request = await new HttpClient().GetAsync($"https://dotabotapi.azurewebsites.net/id/{Convert.ToInt64(messageText)}");
                var response = await request.Content.ReadAsStringAsync();
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: response,
                    cancellationToken: cancellationToken);
            }
            else if (!new Regex(@"\d+").IsMatch(messageText) && !messageText.Contains("https://steamcommunity.com/profiles/"))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Wrong input!",
                    cancellationToken: cancellationToken);  
            }
            command.currentStage = "menu";
            break;
        case "match":
            if (!long.TryParse(messageText, out _))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Wrong input!",
                    cancellationToken: cancellationToken);
            }
            else if (long.TryParse(messageText, out _))
            {
                var idRequest = await new HttpClient().GetAsync($"https://dotabotapi.azurewebsites.net/get_id/{update.Message.From.Username}"); 
                            var requestContent = await idRequest.Content.ReadAsStringAsync();
                            var dotaId = Convert.ToInt32(requestContent);
                            var requestMatch = await new HttpClient().GetAsync($"https://dotabotapi.azurewebsites.net/match/{Convert.ToInt64(messageText)}/{dotaId}");
                            var responseMatch = await requestMatch.Content.ReadAsStringAsync();
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: responseMatch,
                                cancellationToken: cancellationToken);
            }
            command.currentStage = "menu";
            break;
        case "pro":
            var heroId = HeroNames.GetID(messageText);
            var requestPro = await new HttpClient().GetAsync($"https://dotabotapi.azurewebsites.net/hero_promatches/{heroId}");
            var responsePro = await requestPro.Content.ReadAsStringAsync();
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: responsePro,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            command.currentStage = "menu";
            break;
        case "counterpick":
            var hero = HeroNames.GetID(messageText);
            var requestCT = await new HttpClient().GetAsync($"https://dotabotapi.azurewebsites.net/heroes/{hero}/ctpick");
            var responseCT = await requestCT.Content.ReadAsStringAsync();
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: responseCT,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            command.currentStage = "menu";
            break;
    }
    if (command.currentStage == "menu")
    {
        switch (messageText)
            {
                case "/start" or "/set":
                    command.ChangeStage("start");
                    await new HttpClient().DeleteAsync($"https://dotabotapi.azurewebsites.net/remove_id/{update.Message.From.Username}"); 
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Please, log in by entering ID from Dota 2. Also you can send your steam profile link" ,
                        cancellationToken: cancellationToken);
                    break;
                case "/match":
                    command.ChangeStage("match");
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Please, enter match id:",
                        cancellationToken: cancellationToken);
                    break;
                case "/hero":
                    var idrequest = await new HttpClient().GetAsync($"https://dotabotapi.azurewebsites.net/get_id/{update.Message.From.Username}"); 
                    var responseID = await idrequest.Content.ReadAsStringAsync();
                    var acc_id = Convert.ToInt32(responseID);
                    var requestHero = await new HttpClient().GetAsync($"https://dotabotapi.azurewebsites.net/heroes/{acc_id}");
                    var responseHero = await requestHero.Content.ReadAsStringAsync();
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: responseHero,
                        cancellationToken: cancellationToken);
                    break;
                case "/promatches":
                    command.ChangeStage("pro");
                    KeyboardButton[][] keyboardButtons =
                        HeroNames.Names.Values.Select(value => new KeyboardButton[] { new KeyboardButton(value) }).ToArray();
                    ReplyKeyboardMarkup replyMarkup = new ReplyKeyboardMarkup(keyboardButtons);
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Please, choose hero:",
                        replyMarkup: replyMarkup,
                        cancellationToken: cancellationToken);
                    break;
                case "/counterpicks":
                    command.ChangeStage("counterpick");
                    KeyboardButton[][] keyboard =
                        HeroNames.Names.Values.Select(value => new KeyboardButton[] { new KeyboardButton(value) }).ToArray();
                    ReplyKeyboardMarkup replyM = new ReplyKeyboardMarkup(keyboard);
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Please, choose hero:",
                        replyMarkup: replyM,
                        cancellationToken: cancellationToken);
                    break;
            }
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
}