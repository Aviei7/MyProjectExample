# AstroBot

AstroBot is a Telegram bot written in C#/.NET 8 that automatically sends astrological updates to selected Telegram chats.

The bot collects information about moon phases, eclipses, and retrograde planets, formats it into Ukrainian-language messages, and sends the result either manually from the admin menu or automatically by schedule.

## What the bot does

AstroBot helps automate regular astrological posts for Telegram chats.

Main use cases:

- send the current month’s moon phase information;
- send information about retrograde planets for the current month;
- send a combined astro message to a selected chat;
- create scheduled monthly message delivery;
- view active scheduled jobs;
- stop scheduled jobs from the admin panel.

## Features

- Telegram bot with custom admin keyboard.
- Admin-only access control.
- Manual message sending to configured chats.
- Test sending directly to the admin.
- Monthly scheduled sending by day and time.
- Multiple active schedules for different chats.
- Active schedule list with next execution time.
- Schedule cancellation through inline buttons.
- Kyiv timezone support.
- HTML parsing of external astro data.
- Ukrainian-language output messages.

## Tech stack

- C#
- .NET 8
- Telegram.Bot
- HtmlAgilityPack
- TimeZoneConverter
- Telegram Bot API

## Project structure

```text
AstroBot/
├── AstroBot.sln
├── README.md
└── AstroBot/
    ├── AstroBot.csproj
    ├── Program.cs
    ├── AstroService.cs
    ├── ControlChats/
    │   ├── ControlBot.cs
    │   ├── Keyboards.cs
    │   ├── MainChats.cs
    │   └── ScheduleControl.cs
    ├── MoonPhase/
    │   ├── MoonPhaseBuild.cs
    │   └── PhaseResult.cs
    ├── RetroPlanet/
    │   ├── RetroPeriod.cs
    │   ├── RetroPlanetBuild.cs
    │   └── RetroPlanetResult.cs
    └── ScheduleSendMessage/
        ├── ScheduledJob.cs
        └── ScheduledSender.cs
```

## Main components

### `Program.cs`

Application entry point.

It:

- configures console encoding;
- sets the Kyiv timezone;
- creates the Telegram bot client;
- registers the `/start` command;
- starts receiving Telegram updates.

### `AstroService.cs`

Service responsible for building and sending the final astro message.

It combines:

- moon phase message from `MoonPhaseBuild`;
- retrograde planet message from `RetroPlanetBuild`.

Then it sends the final result to Telegram using HTML parse mode.

### `ControlChats`

Contains bot control logic.

| File | Purpose |
|---|---|
| `ControlBot.cs` | Handles Telegram updates, messages, callbacks, admin checks, and manual sending. |
| `Keyboards.cs` | Stores reply keyboards for the admin panel, chat selection, and cancel actions. |
| `MainChats.cs` | Stores configured admin IDs and target chat IDs. |
| `ScheduleControl.cs` | Handles schedule creation, validation, job list display, and schedule cancellation. |

### `MoonPhase`

Responsible for moon phase parsing and message building.

`MoonPhaseBuild` downloads moon phase data from Astro-Seek, parses phase rows, detects eclipses, groups moon phase date ranges, translates phase names, and builds the final Ukrainian message.

### `RetroPlanet`

Responsible for retrograde planet parsing and message building.

`RetroPlanetBuild` downloads retrograde planet data from Astro-Seek, parses retrograde periods, merges overlapping periods, filters events for the current month, translates planet names, and builds the final Ukrainian message.

### `ScheduleSendMessage`

Contains background scheduling logic.

| File | Purpose |
|---|---|
| `ScheduledJob.cs` | Model for a running scheduled job. Stores ID, name, next run time, cancellation token, and task. |
| `ScheduledSender.cs` | Runs scheduled sending loop and calculates the next run date. |

## Bot commands and admin menu

The bot registers the `/start` command.

After `/start`, an admin sees the main control menu:

- `Відправити у чат`
- `Тест`
- `Запустити регламент`
- `Зупинити регламент`
- `Список регламентів`

Non-admin users receive a message that they are not allowed to use bot actions.

## Scheduling flow

To create a schedule:

1. Admin clicks `Запустити регламент`.
2. Bot asks for the day of the month.
3. Admin enters a day number.
4. Bot asks for the time.
5. Admin enters time, for example `9:00`.
6. Bot asks for the target group.
7. Admin selects the group.
8. Bot starts a background scheduled job.

The schedule runs monthly. If the selected day does not exist in a month, the bot uses the last available day of that month.

Example: if the schedule day is `31`, then in February the bot will use the last day of February.

## Configuration

Before running the bot, configure these values in the source code.

### Telegram bot token

In `Program.cs`, replace:

```csharp
TelegramBotClient bot = new TelegramBotClient("TOKEN");
```

with your real bot token from BotFather.

Recommended improvement: move the token to environment variables or user secrets instead of storing it directly in code.

### Admin and chat IDs

In `ControlChats/MainChats.cs`, configure:

```csharp
public static Dictionary<string, long> AdminChat = new()
{
    { "Admin1", 123456789 }
};

public static Dictionary<string, long> GroupList = new()
{
    { "MyChat", -1001234567890 }
};

public static Dictionary<string, long> SendForScheduleChats = new()
{
    { "MyChat", -1001234567890 }
};
```

`AdminChat` controls who can use the bot.

`GroupList` controls chats available for manual sending.

`SendForScheduleChats` controls chats available for scheduled sending.

## How to run locally

### Requirements

- .NET 8 SDK
- Telegram bot token
- Telegram user/chat IDs configured in `MainChats.cs`

### Run

From the `AstroBot` solution folder:

```bash
dotnet restore
dotnet build
dotnet run --project AstroBot/AstroBot.csproj
```

After startup, the console should show that the bot has started.

Open Telegram and send:

```text
/start
```

to the bot.

## Security notes

Do not commit real Telegram bot tokens, admin IDs, or private chat IDs to a public repository.

Current code keeps these values directly in source files. For real deployment, move them to:

- environment variables;
- `.NET user-secrets`;
- config files excluded by `.gitignore`;
- secret storage on the server.

## Possible improvements

- Move bot token and chat IDs to configuration.
- Add `appsettings.json` support.
- Add dependency injection.
- Replace `async void` with `Task` where possible.
- Add logging instead of `Console.WriteLine`.
- Add error handling for failed HTML parsing.
- Add tests for date parsing and schedule calculation.
- Persist schedules so they survive application restart.
- Add Dockerfile for VPS deployment.
- Add GitHub Actions build check.
- Improve formatting and line breaks in source files.

## Status

This is a learning/pet project for automating Telegram astro content delivery. The bot already supports manual sending, admin controls, parsing, and scheduled delivery, but configuration and deployment parts should be improved before real production use.
