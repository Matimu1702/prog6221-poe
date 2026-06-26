using System;
using System.Collections.Generic;

namespace CyberBot
{
    // ─────────────────────────────────────────────────────────────────────────
    // ResponseSystem  – keyword / sentiment-driven chatbot responses (Parts 1-2)
    // ─────────────────────────────────────────────────────────────────────────
    public static class ResponseSystem
    {
        // ── Keyword → response dictionary ─────────────────────────────────────
        private static readonly Dictionary<string, string> KeywordResponses =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "password",     "Use a strong password with at least 12 characters, mixing letters, numbers and symbols. Never reuse passwords across sites." },
            { "phishing",     "Phishing emails trick you into revealing sensitive info. Always check the sender's address and never click suspicious links." },
            { "2fa",          "Two-Factor Authentication (2FA) adds a second layer of security. Enable it on all important accounts." },
            { "two-factor",   "Two-Factor Authentication (2FA) adds a second layer of security. Enable it on all important accounts." },
            { "malware",      "Malware is malicious software. Keep your OS and antivirus up to date and avoid downloading files from unknown sources." },
            { "firewall",     "A firewall monitors incoming and outgoing network traffic. Always keep it enabled to block unauthorised access." },
            { "vpn",          "A VPN encrypts your internet connection, protecting your data on public Wi-Fi." },
            { "ransomware",   "Ransomware encrypts your files and demands payment. Regularly back up your data to minimise damage." },
            { "social engineering", "Social engineering manipulates people into revealing confidential information. Always verify identities before sharing data." },
            { "privacy",      "Review your privacy settings on social media and apps regularly to limit data exposure." },
            { "encryption",   "Encryption converts your data into unreadable code. Use HTTPS websites and encrypted messaging apps." },
            { "backup",       "Regular backups protect your data from ransomware and hardware failures. Use the 3-2-1 rule: 3 copies, 2 media types, 1 offsite." },
            { "antivirus",    "Keep your antivirus software up to date and run regular scans to detect threats early." },
            { "wifi",         "Avoid using public Wi-Fi for sensitive tasks. If you must, use a VPN to encrypt your traffic." },
            { "update",       "Software updates patch security vulnerabilities. Enable automatic updates whenever possible." },
            { "scam",         "If something sounds too good to be true, it probably is. Verify before you trust." },
        };

        // ── Sentiment words ───────────────────────────────────────────────────
        private static readonly HashSet<string> PositiveWords =
            new(StringComparer.OrdinalIgnoreCase)
            { "great", "good", "happy", "love", "excellent", "awesome", "thanks", "thank" };

        private static readonly HashSet<string> NegativeWords =
            new(StringComparer.OrdinalIgnoreCase)
            { "bad", "hate", "terrible", "worried", "scared", "anxious", "help", "danger", "hack", "hacked" };

        // ── Main entry point ──────────────────────────────────────────────────
        public static string GetResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Please type a message so I can help you.";

            string lower = input.ToLower();

            // Sentiment detection
            string sentiment = DetectSentiment(lower);
            if (!string.IsNullOrEmpty(sentiment))
                return sentiment;

            // Keyword matching
            foreach (var kv in KeywordResponses)
                if (lower.Contains(kv.Key.ToLower()))
                    return kv.Value;

            // Greetings
            if (lower.Contains("hello") || lower.Contains("hi") || lower.Contains("hey"))
                return "Hello! How can I help you stay safe online today?";

            if (lower.Contains("bye") || lower.Contains("goodbye"))
                return "Stay safe online! Goodbye! 👋";

            if (lower.Contains("how are you") || lower.Contains("how r u"))
                return "I'm fully operational and ready to help you with cybersecurity questions!";

            // Fallback
            return "I'm not sure about that yet. Try asking me about passwords, phishing, VPNs, malware, or other cybersecurity topics!";
        }

        // ── Sentiment helper ──────────────────────────────────────────────────
        private static string DetectSentiment(string input)
        {
            foreach (var word in PositiveWords)
                if (input.Contains(word))
                    return "I'm glad you're feeling positive! Staying informed about cybersecurity keeps you even safer. 😊";

            foreach (var word in NegativeWords)
                if (input.Contains(word))
                    return "I understand you're concerned. Cybersecurity threats are serious, but knowledge is your best defence. Let me help!";

            return string.Empty;
        }
    }
}
