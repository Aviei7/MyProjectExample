#AstroBot

## Overview
AstroBot is a Telegram bot that delivers moon phases, eclipses, and planetary retrograde updates to chats on a schedule.

## Problem it solves
Manual tracking of lunar phases and retrogrades is inconvenient.
AstroBot automates this process and delivers structured information directly to Telegram.

### Key features
- Scheduled message delivery by date, time, and chat
- Support for multiple simultaneous schedules
- Admin-only access to bot controls
- View active schedules with next execution time
- Cancel running schedules via inline buttons

### Architecture 
- Background scheduler with cancellation support 
- Centralized task registry
- Built-in Telegram keyboards for control 
- Time zone-aware execution

### Tech stack
- C# / .NET
- Telegram Bot API
- Docker
- Background scheduling
- Website parsing

### Deployment
- Docker-based deployment
- Can run 24/7 on a VPS
