using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstroBot.ScheduleSendMessage
{
    public sealed class ScheduledJob
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; init; } = "";
        public DateTimeOffset NextRun { get; set; }
        public CancellationTokenSource Cts { get; set; }
        public Task? Task { get; set; }
    }
}
