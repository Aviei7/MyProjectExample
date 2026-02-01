using AstroBot.ControlChats;
using AstroBot.MoonPhase;
using AstroBot.RetroPlanet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AstroBot
{
    public class AstroService
    {
        private readonly ITelegramBotClient bot;
        private readonly MoonPhaseBuild moonPhasebuilder;
        private readonly RetroPlanetBuild retroPlanetBuilder;

        public AstroService(ITelegramBotClient bot)
        {
            this.bot = bot;
            moonPhasebuilder = new MoonPhaseBuild();
            retroPlanetBuilder = new RetroPlanetBuild();
        }

        public async Task SendAstroMessage(long chatId, DateTime DateNow)
        {
            var retroPlanetFinalMessage = await retroPlanetBuilder.CreateMessageForRetroPlanet(DateNow);

            var moonPhaseFinalMessage = await moonPhasebuilder.CreateMessageForMoonPhase(DateNow);

            var fullMessage = FullMessage(moonPhaseFinalMessage, retroPlanetFinalMessage);

            SendMessage(chatId, fullMessage);
        }

        public async Task SendScheduleInfoMessage(string nextRunText, TimeSpan delay, string chatName, bool isToMessage)
        {
            var chatID = MainChats.SendForScheduleChats["AstroPhaseBot"];
            string finalMessage;


            if (isToMessage)
            {
                finalMessage = $"Повідомлення у чат {chatName} відправлено";
            }
            else
            {
                finalMessage = $"Регламент запущено! Наступне виконання - {nextRunText}, очікування до виконання = {delay.Days:D2} днів, {delay.Hours:D2} годин {delay.Minutes:D2} хвилин";
            }


          Console.WriteLine(finalMessage);

            await bot.SendMessage(chatID, finalMessage, replyMarkup: Keyboards.AdminMenu);

        }

        private string FullMessage(string first, string second)
        {
            StringBuilder sbFull = new StringBuilder();

            sbFull.AppendLine(first);
            sbFull.AppendLine(second);

            return sbFull.ToString();
        }

        private async void SendMessage(long chatId, string finalMessage)
        {
            await bot.SendMessage(chatId, finalMessage, parseMode: ParseMode.Html);
        }

    }
}
