using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CyberBot
{
    // ─────────────────────────────────────────────────────────────────────────
    // NlpEngine  – classifies user intent and extracts key entities
    //              Task 3: NLP Simulation (string manipulation + regex)
    // ─────────────────────────────────────────────────────────────────────────
    public static class NlpEngine
    {
        // ── Intent enum ───────────────────────────────────────────────────────
        public enum Intent
        {
            Unknown,
            AddTask,
            ViewTasks,
            CompleteTask,
            DeleteTask,
            SetReminder,
            StartQuiz,
            ShowLog,
            GeneralChat
        }

        // ── Intent keyword maps ───────────────────────────────────────────────
        private static readonly Dictionary<Intent, string[]> IntentKeywords = new()
        {
            { Intent.AddTask,      new[] { "add task", "create task", "new task", "add a task", "set task",
                                           "add reminder", "remind me to", "remind me about", "remember to" } },
            { Intent.ViewTasks,    new[] { "view tasks", "show tasks", "list tasks", "my tasks",
                                           "show my tasks", "what tasks", "see tasks" } },
            { Intent.CompleteTask, new[] { "complete task", "mark done", "finished task", "task done",
                                           "mark task", "mark as done", "mark as complete", "tick off" } },
            { Intent.DeleteTask,   new[] { "delete task", "remove task", "cancel task", "drop task" } },
            { Intent.SetReminder,  new[] { "set reminder", "remind me in", "remind me on", "add reminder" } },
            { Intent.StartQuiz,    new[] { "start quiz", "play quiz", "quiz me", "begin quiz",
                                           "cybersecurity quiz", "test my knowledge", "take the quiz" } },
            { Intent.ShowLog,      new[] { "show log", "activity log", "show activity", "what have you done",
                                           "recent actions", "show history", "action log", "what did you do" } },
        };

        // ── Classify intent ───────────────────────────────────────────────────
        public static Intent Classify(string input)
        {
            string lower = input.ToLower().Trim();

            foreach (var kv in IntentKeywords)
                foreach (var keyword in kv.Value)
                    if (lower.Contains(keyword))
                        return kv.Key;

            return Intent.GeneralChat;
        }

        // ── Extract task title from free-text input ───────────────────────────
        // e.g. "Add a task to enable 2FA" → "Enable 2FA"
        public static string ExtractTaskTitle(string input)
        {
            // Remove leading intent phrases
            string[] strips = { "add task", "create task", "new task", "add a task", "set task",
                                 "remind me to", "remind me about", "remember to",
                                 "add reminder for", "set reminder for", "set reminder to",
                                 "add a reminder to", "add a reminder for" };

            string lower = input.ToLower();
            foreach (var s in strips)
            {
                int idx = lower.IndexOf(s, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string rest = input[(idx + s.Length)..].Trim().TrimStart(':', '-', ' ');
                    if (!string.IsNullOrWhiteSpace(rest))
                        return Capitalise(rest);
                }
            }

            // Fallback: capitalise whole input
            return Capitalise(input);
        }

        // ── Extract reminder offset in days from phrases like "in 3 days", "in 7 days" ──
        public static int? ExtractReminderDays(string input)
        {
            // "in N days/day"
            var m = Regex.Match(input, @"in\s+(\d+)\s+days?", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int days))
                return days;

            // "tomorrow"
            if (input.Contains("tomorrow", StringComparison.OrdinalIgnoreCase))
                return 1;

            // "next week"
            if (input.Contains("next week", StringComparison.OrdinalIgnoreCase))
                return 7;

            // "in a week"
            if (input.Contains("in a week", StringComparison.OrdinalIgnoreCase))
                return 7;

            return null;
        }

        // ── Extract a task ID number from input ───────────────────────────────
        public static int? ExtractId(string input)
        {
            var m = Regex.Match(input, @"\b(\d+)\b");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int id))
                return id;
            return null;
        }

        // ── Capitalise first letter helper ────────────────────────────────────
        private static string Capitalise(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s[1..];
        }
    }
}
