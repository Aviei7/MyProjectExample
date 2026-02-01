using AstroBot.ControlChats;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TimeZoneConverter;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AstroBot.ScheduleSendMessage
{
    public class ScheduledSender
    {
        public async Task RunAsync(CancellationToken token, ITelegramBotClient bot, long scheduleChatID, int dateExec, TimeSpan targetTime, ScheduledJob job)
        {
            var astroService = new AstroService(bot);
            var timeZone = TZConvert.GetTimeZoneInfo("Europe/Kyiv");
            Console.WriteLine($"Start_Schedule in chat ({job.Name})");

            while (!token.IsCancellationRequested)
            {
                var dateNow = DateTime.Now;
                var nowKyiv = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);

                var nextRunLocal = NextRun(timeZone, nowKyiv, targetTime, dateExec);

                var delay = nextRunLocal - dateNow;
                var nextRunText = nextRunLocal.ToString("dd.MM.yyyy HH:mm:ss");
                await astroService.SendScheduleInfoMessage(nextRunText, delay, job.Name, false);

                job.NextRun = nextRunLocal;

                try
                {
                    await Task.Delay(delay, token);
                }
                catch
                {
                    break;
                }

                await astroService.SendAstroMessage(scheduleChatID, nextRunLocal.DateTime);

                await astroService.SendScheduleInfoMessage(nextRunText, delay, job.Name, true);
            }
        }


        public DateTimeOffset NextRun(TimeZoneInfo timeZone, DateTimeOffset nowKyiv, TimeSpan targetTime, int dateExec)
        {
            int dayInMonth = Math.Min(dateExec, DateTime.DaysInMonth(nowKyiv.Year, nowKyiv.Month));

            var localCandidate = new DateTime(nowKyiv.Year, nowKyiv.Month, dayInMonth,
                                      targetTime.Hours, targetTime.Minutes, 0,
                                      DateTimeKind.Unspecified);

            var nextRun = new DateTimeOffset(localCandidate, timeZone.GetUtcOffset(localCandidate));


            if (nextRun <= nowKyiv)
            {
                var nextMonth = nowKyiv.AddMonths(1);
                int dayInNextMonth = Math.Min(dateExec, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));

                localCandidate = new DateTime(nextMonth.Year, nextMonth.Month, dayInNextMonth,
                                      targetTime.Hours, targetTime.Minutes, 0,
                                      DateTimeKind.Unspecified);

                nextRun = new DateTimeOffset(localCandidate, timeZone.GetUtcOffset(localCandidate));
            }

            return nextRun;
        }

    }
}
