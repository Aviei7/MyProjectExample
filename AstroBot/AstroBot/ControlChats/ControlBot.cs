using AstroBot.ScheduleSendMessage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace AstroBot.ControlChats
{

    public class ControlBot
    {
        private readonly ITelegramBotClient _bot;
        private readonly DateTime _dateNow;

        private readonly AstroService astroService;
        private readonly ScheduleControl _scheduleControl;

        private bool isSendMessageToChat = false;

        public ControlBot(ITelegramBotClient bot, DateTime dateNow, TimeZoneInfo timeZone)
        {
            astroService = new AstroService(bot);
            _scheduleControl = new ScheduleControl(bot, dateNow, timeZone);
            _bot = bot;
            _dateNow = dateNow;
        }

       

        public async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken token)
        {

            // 1. Нажатия кнопок
            if (update.CallbackQuery != null)
            {
                await HandleCallback(update);
                return;
            }

            // 2. Обычные сообщения
            if (update.Message != null)
            {
                await HandleMessage(update);
                return;
            }

        }


        private async Task HandleCallback(Update update)
        {
            var callback = update.CallbackQuery;
            if (callback == null)
                return;

            long userChatId = callback.From.Id;             
            long chatId = callback.Message.Chat.Id;         

            if (callback.Data == "disabled")
            {
                await _bot.AnswerCallbackQuery(callback.Id, "Недоступно", showAlert: false);
                return;
            }

            if (IsAdmin(userChatId))
            {
                await _scheduleControl.ScheduleCallbackLogic(userChatId, callback);
            }
        }

        private async Task HandleMessage(Update update)
        {
            long userChatId;
            ChatFullInfo chatFullInfo;

            (userChatId, chatFullInfo) = await UpdateChatVar(update.Message, _bot);

            Console.WriteLine($"User: ({chatFullInfo.Username}). Write message: ({update.Message.Text}). ChatID: ({userChatId}). Time: ({update.Message.Date})");

            var msg = update.Message;


            if (msg == null)
                return;

            if (msg?.Text == "/start")
            {
                if (IsAdmin(userChatId))
                {
                    await _bot.SendMessage(
                       userChatId,
                       "Оберіть дію:",
                       replyMarkup: Keyboards.AdminMenu
                   );
                }
                else
                {
                    await _bot.SendMessage(
                       userChatId,
                       "Ви не є адміністратором, будь-які дії заборонено."
                   );
                }
                return;
            }


            if (IsAdmin(userChatId))
            {
                await AdminPanel(msg, update, userChatId, chatFullInfo);
                await _scheduleControl.ScheduleMsgLogic(userChatId, msg, update);
            }
        }

        private async Task AdminPanel(Message msg, Update update, long userChatId, ChatFullInfo chatFullInfo)
        {

            if (msg?.Text == "Скасувати")
            {
                await SendWithAdminKeyboard(userChatId, $"Операцію скасовано");
            }

            if (msg?.Text == "Тест")
            {
                await astroService.SendAstroMessage(userChatId, _dateNow);
                await _bot.SendMessage(msg.Chat.Id, $"Повідомлення у чат {chatFullInfo.Username} відправлено ");
                return;
            }

            if (msg?.Text == "Відправити у чат")
            {
                await _bot.SendMessage(userChatId, "Оберіть чат:", replyMarkup: Keyboards.SendMessageList);
                isSendMessageToChat = true;
                return;
            }

            if (isSendMessageToChat && msg?.Text != null && MainChatDictionary(msg.Text))
            {
                userChatId = GetChatIDForGroupList(msg.Text);
                await astroService.SendAstroMessage(userChatId, _dateNow);

                await SendWithAdminKeyboard(userChatId, $"Повідомлення у чат {msg.Text} відправлено ");
                isSendMessageToChat = false;
            }
        }


        public Task ErrorHandler(ITelegramBotClient bot, Exception ex, CancellationToken token)
        {
            Console.WriteLine(ex);
            return Task.CompletedTask;
        }

        private async Task<(long, ChatFullInfo)> UpdateChatVar(Message msg, ITelegramBotClient bot)
        {
            var userChatId = msg.Chat.Id;
            var chatFullInfo = await bot.GetChat(userChatId, cancellationToken: CancellationToken.None);

            return (userChatId, chatFullInfo);
        }

        private async Task SendWithAdminKeyboard(long userChatId, string text)
        {
            await _bot.SendMessage(
                     MainChats.SendForScheduleChats["AstroPhaseBot"],
                     text,
                     replyMarkup: Keyboards.AdminMenu
                 );
        }

        private bool MainChatDictionary(string Name)
        {
            return MainChats.GroupList.ContainsKey(Name);
        }

        private long GetChatIDForGroupList(string Name)
        {
            return MainChats.GroupList[Name];
        }

        private bool IsAdmin(long userChatId)
        {
            return MainChats.AdminChat.ContainsValue(userChatId);
        }
    }
}
