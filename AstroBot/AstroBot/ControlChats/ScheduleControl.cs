using AstroBot.ScheduleSendMessage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Reflection.Metadata.BlobBuilder;

namespace AstroBot.ControlChats
{
    public class ScheduleControl
    {
        private readonly ScheduledSender scheduledSender;

        private readonly DateTime _dateNow;
        private readonly ITelegramBotClient _bot;
        private readonly TimeZoneInfo _timeZone;


        private int daySchedule;
        private TimeSpan timeSchedule;
        private string groupName;

        public ScheduleControl(ITelegramBotClient bot, DateTime dateNow, TimeZoneInfo timeZone)
        {
            scheduledSender = new ScheduledSender();
            _dateNow = dateNow;
            _timeZone = timeZone;
            _bot = bot;
        }

        private static readonly Dictionary<string, ScheduledJob> _jobs = new();

        enum ScheduleState
        {
            None,
            WaitingForDay,
            WaitingForTime,
            WaitingForGroup
        }
        private ScheduleState _scheduleState = ScheduleState.None;

        private async Task StartScheduleSender(int dateExec, TimeSpan targetTime, string nameGroup, long userChatId)
        {
            StopScheduleSender(nameGroup);
            CancellationTokenSource _scheduleCts = new CancellationTokenSource();
            var scheduleChatID = MainChats.SendForScheduleChats[nameGroup];

            var nextRun = scheduledSender.NextRun(_timeZone, _dateNow, targetTime, dateExec);

            ScheduledJob job = new ScheduledJob
            {
                Name = nameGroup,
                Cts = _scheduleCts,
            };


            job.Task = Task.Run(async () =>
            {
                await scheduledSender.RunAsync(
                    job.Cts.Token,
                    _bot,
                    scheduleChatID,
                    dateExec,
                    targetTime,
                    job
                );
            });

            _jobs[nameGroup] = job;
        }

        public async Task ScheduleCallbackLogic(long userChatId, CallbackQuery callBack)
        {

            if (callBack.Data?.StartsWith("stop:") == true)
            {
                var idText = callBack.Data.Replace("stop:", "");

                if (Guid.TryParse(idText, out var jobId))
                {
                    var job = _jobs.Values.FirstOrDefault(j => j.Id == jobId);

                    if (job == null)
                    {
                        await _bot.AnswerCallbackQuery(callBack.Id, "Регламент вже не активний");
                        return;
                    }

                    job.Cts.Cancel();
                    job.Cts.Dispose();
                    _jobs.Remove(job.Name);

                    await _bot.AnswerCallbackQuery(
                          callBack.Id,
                          $"Регламент «{job.Name}» зупинено"
                    );

                    await AdminKeyboard(userChatId, $"Регламент «{job.Name}» зупинено");

                    if (job != null)
                    {
                        await JobList(userChatId, "disabled", "Список активних регламентів:");
                    }
                }
            }


        }

        public async Task ScheduleMsgLogic(long userChatId, Message msg, Update update)
        {

            if (msg?.Text == "Скасувати")
            {
                _scheduleState = ScheduleState.None;
                return;
            }


            if (msg?.Text == "Запустити регламент")
            {
                await CancelKeyboard(userChatId, "Напишіть день початку: ");

                _scheduleState = ScheduleState.WaitingForDay;
                return;
            }

            if (_scheduleState == ScheduleState.WaitingForDay && msg != null)
            {

                daySchedule = await CheckWritingDay(msg, userChatId);
                return;

            }

            if (_scheduleState == ScheduleState.WaitingForTime && msg != null)
            {

                timeSchedule = await CheckWritingTime(msg, userChatId);
                return;

            }

            if (_scheduleState == ScheduleState.WaitingForGroup && msg != null)
            {
                groupName = await CheckWritingGroup(msg, userChatId);

                // запускаем расписание
                await StartScheduleSender(daySchedule, timeSchedule, groupName, userChatId);

                // сбрасываем состояние
                _scheduleState = ScheduleState.None;

                return;
            }

            if (msg?.Text == "Зупинити регламент")
            {

                if (_jobs.Count == 0)
                {
                    await _bot.SendMessage(userChatId, "Немає активних регламентів");
                    return;
                }


                await JobList(userChatId, "stop", "Оберіть регламент для зупинки:");

                return;
            }

            if (msg?.Text == "Список регламентів")
            {
                if (_jobs.Count == 0)
                {
                    await _bot.SendMessage(userChatId, "Немає активних регламентів");
                    return;
                }

                await JobList(userChatId, "disabled", "Список активних регламентів:");

                return;
            }
        }

        private async Task<int> CheckWritingDay(Message msg, long userChatId)
        {
            string text = msg.Text;
            int day = 0;

            if (TryParseDay(text, out day))
            {
                await _bot.SendMessage(userChatId, $"Прийнято! День = {day}");

                await _bot.SendMessage(userChatId, "Оберіть час: ");

                _scheduleState = ScheduleState.WaitingForTime;

                return day;
            }
            else
            {
                await _bot.SendMessage(userChatId, "Некоректний день. Введіть число від 1 до кількості днів у місяці.");
                return day;
            }
        }

        public bool TryParseDay(string input, out int day)
        {
            day = 0;

            if (!int.TryParse(input, out int parsed))
                return false;

            DateTime now = DateTime.Now;
            int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

            if (parsed >= 1 && parsed <= daysInMonth)
            {
                day = parsed;
                return true;
            }

            return false;
        }

        private async Task<TimeSpan> CheckWritingTime(Message msg, long userChatId)
        {
            string text = msg.Text;

            TimeSpan Time = TimeSpan.Zero;

            if (TryParseTime(text, out Time))
            {
                await _bot.SendMessage(userChatId, $"Прийнято! Час = {Time}");

                await _bot.SendMessage(userChatId, "Оберіть групу: ", replyMarkup: Keyboards.SendMessageList);

                _scheduleState = ScheduleState.WaitingForGroup;

                return Time;
            }
            else
            {
                await _bot.SendMessage(userChatId, "Некоректний час. Введіть час наприклад 9:00.");
                return Time;
            }
        }

        public bool TryParseTime(string text, out TimeSpan time)
        {
            return TimeSpan.TryParse(text, out time);
        }

        private async Task<string> CheckWritingGroup(Message msg, long userChatId)
        {
            string groupName = msg.Text.Trim();

            if (!MainChats.SendForScheduleChats.ContainsKey(groupName))
            {
                await _bot.SendMessage(userChatId, "Такої групи нема. Спробуйте ще раз.");
                return groupName;
            }
            return groupName;
        }

        private void StopScheduleSender(string nameGroup)
        {
            if (_jobs.TryGetValue(nameGroup, out var oldJob))
            {
                oldJob.Cts.Cancel();
                oldJob.Cts.Dispose();
                _jobs.Remove(nameGroup);
            }
        }

        private async Task JobList(long userChatId, string operationType, string textMsg)
        {
            var keyboard = new InlineKeyboardMarkup(_jobs.Values.Select(job =>
                           new[] {
                                    InlineKeyboardButton.WithCallbackData(
                                    $"🛑 {job.Name}",
                                    operationType == "stop"
                                    ? $"stop:{job.Id}" 
                                    : "disabled"
                                    ),
                                    InlineKeyboardButton.WithCallbackData($"⏰ {job.NextRun:dd.MM HH:mm}", operationType=="stop" ? $"stop:{job.Id}" : "disabled")
                           }
                   )
               );

            await _bot.SendMessage(
                userChatId,
                textMsg,
                replyMarkup: keyboard
            );
        }

        private async Task CancelKeyboard(long userChatId, string Text)
        {
            await _bot.SendMessage(
                userChatId,
                Text,
                replyMarkup: Keyboards.CancelAction
            );
        }

        private async Task AdminKeyboard(long userChatId, string Text)
        {
            await _bot.SendMessage(
                userChatId,
                Text,
                replyMarkup: Keyboards.AdminMenu
            );
        }
    }
}
