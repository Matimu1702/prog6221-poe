using System.Collections.Generic;

namespace CyberBot
{
    // ─────────────────────────────────────────────────────────────────────────
    // QuizData  – Task 2: 12+ cybersecurity questions (MC + True/False)
    // ─────────────────────────────────────────────────────────────────────────
    public static class QuizData
    {
        public static List<QuizQuestion> Questions => new()
        {
            // ── Multiple Choice ───────────────────────────────────────────────
            new QuizQuestion
            {
                Text       = "What should you do if you receive an email asking for your password?",
                Options    = new[] { "A) Reply with your password", "B) Delete the email",
                                     "C) Report it as phishing", "D) Ignore it" },
                Answer     = "C",
                Explanation = "Reporting phishing emails helps authorities take down scam operations and protects other users."
            },
            new QuizQuestion
            {
                Text       = "Which of the following makes the strongest password?",
                Options    = new[] { "A) password123", "B) YourBirthday",
                                     "C) P@ssw0rd!", "D) Tr0ub4dor&3#xQ!" },
                Answer     = "D",
                Explanation = "Long, random passphrases mixing upper/lowercase, numbers and symbols are the hardest to crack."
            },
            new QuizQuestion
            {
                Text       = "What does HTTPS indicate in a website URL?",
                Options    = new[] { "A) The site is popular", "B) The connection is encrypted",
                                     "C) The site is free", "D) The server is fast" },
                Answer     = "B",
                Explanation = "HTTPS uses TLS encryption to protect data in transit between your browser and the server."
            },
            new QuizQuestion
            {
                Text       = "What is two-factor authentication (2FA)?",
                Options    = new[] { "A) Using two different passwords", "B) Logging in twice",
                                     "C) A second verification step after the password", "D) A type of firewall" },
                Answer     = "C",
                Explanation = "2FA requires a second proof of identity (e.g. SMS code or authenticator app), making accounts far harder to hijack."
            },
            new QuizQuestion
            {
                Text       = "Which action is SAFEST on public Wi-Fi?",
                Options    = new[] { "A) Online banking", "B) Using a VPN",
                                     "C) Sharing personal files", "D) Logging into social media without VPN" },
                Answer     = "B",
                Explanation = "A VPN encrypts your traffic, shielding it from eavesdroppers on open networks."
            },
            new QuizQuestion
            {
                Text       = "What is ransomware?",
                Options    = new[] { "A) Software that speeds up your PC", "B) A type of antivirus",
                                     "C) Malware that encrypts your files and demands payment", "D) A firewall rule" },
                Answer     = "C",
                Explanation = "Regular offline backups are your best defence against ransomware — they let you restore without paying."
            },
            new QuizQuestion
            {
                Text       = "A stranger calls claiming to be from IT and asks for your login. You should…",
                Options    = new[] { "A) Give them your details", "B) Hang up and report to your real IT team",
                                     "C) Give them only your username", "D) Ask them to email you" },
                Answer     = "B",
                Explanation = "This is a classic social-engineering (vishing) attack. Legitimate IT staff never need your password."
            },
            new QuizQuestion
            {
                Text       = "What does the principle of 'least privilege' mean?",
                Options    = new[] { "A) Give all users admin rights", "B) Never share passwords",
                                     "C) Grant users only the access they need", "D) Use the weakest encryption" },
                Answer     = "C",
                Explanation = "Limiting access reduces the damage an attacker can do if one account is compromised."
            },

            // ── True / False ──────────────────────────────────────────────────
            new QuizQuestion
            {
                Text       = "True or False: Using the same password for multiple sites is safe as long as it's strong.",
                Options    = new[] { "A) True", "B) False" },
                Answer     = "B",
                Explanation = "Password reuse means one data breach can compromise all your accounts. Use a password manager."
            },
            new QuizQuestion
            {
                Text       = "True or False: Antivirus software alone is sufficient to protect against all cyber threats.",
                Options    = new[] { "A) True", "B) False" },
                Answer     = "B",
                Explanation = "Antivirus is just one layer. You also need updates, strong passwords, 2FA, and safe browsing habits."
            },
            new QuizQuestion
            {
                Text       = "True or False: Clicking 'unsubscribe' in a spam email confirms your address to spammers.",
                Options    = new[] { "A) True", "B) False" },
                Answer     = "A",
                Explanation = "Spammers often use unsubscribe clicks to verify active addresses. Mark as spam instead."
            },
            new QuizQuestion
            {
                Text       = "True or False: A firewall can block both incoming and outgoing malicious traffic.",
                Options    = new[] { "A) True", "B) False" },
                Answer     = "A",
                Explanation = "Modern firewalls inspect traffic in both directions, preventing both intrusions and data exfiltration."
            },
        };
    }

    // ── Data model ────────────────────────────────────────────────────────────
    public class QuizQuestion
    {
        public string   Text        { get; set; } = "";
        public string[] Options     { get; set; } = Array.Empty<string>();
        public string   Answer      { get; set; } = "";   // "A", "B", "C", or "D"
        public string   Explanation { get; set; } = "";
    }
}
