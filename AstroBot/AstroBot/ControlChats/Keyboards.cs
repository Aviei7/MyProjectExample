using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace AstroBot.ControlChats
{
    public static class Keyboards
    {
        public static ReplyKeyboardMarkup MainMenu = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Відправити у чат" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardMarkup AdminMenu = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Відправити у чат", "Тест", "Запустити регламент", "Зупинити регламент", "Список регламентів" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardMarkup SendMessageList = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Корисні поради❤️‍🔥", "test_bot_group", "AstroPhaseBot" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardMarkup CancelAction = new ReplyKeyboardMarkup(new[]
       {
            new KeyboardButton[] { "Скасувати" }
        })
        {
            ResizeKeyboard = true
        };
    }
}
