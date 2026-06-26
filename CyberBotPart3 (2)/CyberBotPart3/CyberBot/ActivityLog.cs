using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberBot
{
    // ─────────────────────────────────────────────────────────────────────────
    // ActivityLog  – Task 4: records chatbot actions with timestamps
    // ─────────────────────────────────────────────────────────────────────────
    public static class ActivityLog
    {
        private static readonly List<LogEntry> Entries = new();
        private const int MaxDisplay = 10;

        // ── Add an entry ──────────────────────────────────────────────────────
        public static void Add(string description)
        {
            Entries.Add(new LogEntry
            {
                Timestamp   = DateTime.Now,
                Description = description
            });
        }

        // ── Get the last N entries as formatted strings ────────────────────────
        public static IEnumerable<string> GetRecent(int count = MaxDisplay)
        {
            return Entries
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .Select((e, i) => $"{i + 1}. [{e.Timestamp:HH:mm:ss}] {e.Description}");
        }

        // ── Format for chatbot display ────────────────────────────────────────
        public static string FormatLog()
        {
            var recent = GetRecent().ToList();
            if (recent.Count == 0)
                return "No actions recorded yet. Start chatting, adding tasks, or playing the quiz!";

            return "📋 Here's a summary of recent actions:\n\n" + string.Join("\n", recent);
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp   { get; set; }
        public string   Description { get; set; } = "";
    }
}
