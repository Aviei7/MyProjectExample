using AstroBot;
using AstroBot.ControlChats;
using AstroBot.MoonPhase;
using AstroBot.RetroPlanet;
using HtmlAgilityPack;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TimeZoneConverter;
using static System.Net.Mime.MediaTypeNames;
using static Telegram.Bot.TelegramBotClient;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;
var timeZone = TZConvert.GetTimeZoneInfo("Europe/Kyiv");


using var cts = new CancellationTokenSource();
TelegramBotClient bot = new TelegramBotClient("TOKEN");
var DateNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime; ;
var controller = new ControlBot(bot, DateNow, timeZone);

var me = await bot.GetMe();

await bot.SetMyCommands(
    commands: new[]
    {
        new BotCommand { Command = "start", Description = "Запуск меню" }
    },
    BotCommandScope.Default(),
    "uk" 
);
bot.StartReceiving(controller.UpdateHandler, controller.ErrorHandler);
Console.WriteLine($"{me.Username} запущен");
await Task.Delay(Timeout.Infinite);

Console.ReadLine(); 
cts.Cancel();

