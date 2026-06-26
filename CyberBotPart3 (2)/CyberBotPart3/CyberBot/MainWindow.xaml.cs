using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberBot
{
    // ─────────────────────────────────────────────────────────────────────────
    // MainWindow  – code-behind wiring all four tasks together
    // ─────────────────────────────────────────────────────────────────────────
    public partial class MainWindow : Window
    {
        // ── State ─────────────────────────────────────────────────────────────
        private bool   _waitingForName = true;
        private string _userName       = "User";

        // Task assistant in-memory fallback (when MySQL is unavailable)
        private readonly List<CyberTask> _localTasks = new();
        private int _nextLocalId = 1;
        private bool _dbAvailable = false;

        // Context: are we awaiting a reminder reply for a pending task?
        private bool   _awaitingReminder    = false;
        private string _pendingTaskTitle    = "";
        private string _pendingTaskDesc     = "";

        // Quiz state
        private List<QuizQuestion> _questions     = new();
        private int                _quizIndex     = 0;
        private int                _quizScore     = 0;
        private bool               _quizRunning   = false;
        private bool               _awaitingAnswer = false;

        // ── Constructor ───────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();

            // Attempt DB initialisation
            try
            {
                DatabaseManager.Initialise();
                _dbAvailable = true;
            }
            catch { _dbAvailable = false; }

            // Welcome message in chat
            AppendBotMessage("🛡️ Welcome to CyberBot! I'm your cybersecurity awareness assistant.");
            AppendBotMessage("What's your name?");

            // Load any existing tasks
            RefreshTaskList();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TAB 1 – CHAT
        // ══════════════════════════════════════════════════════════════════════

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendChat_Click(sender, e);
        }

        private void SendChat_Click(object sender, RoutedEventArgs e)
        {
            string text = ChatInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AppendUserMessage(text);
            ChatInput.Clear();

            // ── Name collection ───────────────────────────────────────────────
            if (_waitingForName)
            {
                _userName = text;
                _waitingForName = false;
                UserNameBlock.Text = $"👤 {_userName}";
                AppendBotMessage($"Nice to meet you, {_userName}! 👋 I can help you with:");
                AppendBotMessage("• Cybersecurity advice  •  Task management\n• A security quiz       •  Activity log\n\nType anything to get started, or navigate the tabs above!");
                ActivityLog.Add($"User '{_userName}' started a session.");
                return;
            }

            // ── Reminder follow-up ────────────────────────────────────────────
            if (_awaitingReminder)
            {
                HandleReminderReply(text);
                return;
            }

            // ── NLP intent classification ─────────────────────────────────────
            var intent = NlpEngine.Classify(text);

            switch (intent)
            {
                case NlpEngine.Intent.AddTask:
                    HandleAddTaskViaChat(text);
                    break;

                case NlpEngine.Intent.ViewTasks:
                    HandleViewTasks();
                    break;

                case NlpEngine.Intent.CompleteTask:
                    HandleCompleteTaskViaChat(text);
                    break;

                case NlpEngine.Intent.DeleteTask:
                    HandleDeleteTaskViaChat(text);
                    break;

                case NlpEngine.Intent.SetReminder:
                    HandleSetReminderViaChat(text);
                    break;

                case NlpEngine.Intent.StartQuiz:
                    MainTabs.SelectedIndex = 2;
                    StartQuizInternal();
                    AppendBotMessage("🎮 Switching to the Quiz tab – good luck!");
                    ActivityLog.Add("User started the quiz via chat command.");
                    break;

                case NlpEngine.Intent.ShowLog:
                    MainTabs.SelectedIndex = 3;
                    RefreshLogPanel();
                    AppendBotMessage("📜 Switching to the Activity Log tab.");
                    break;

                default:
                    // General cybersecurity conversation
                    string response = ResponseSystem.GetResponse(text);
                    AppendBotMessage(response);
                    ActivityLog.Add($"NLP general chat: '{TruncateForLog(text)}'");
                    break;
            }
        }

        // ── Chat intent handlers ──────────────────────────────────────────────

        private void HandleAddTaskViaChat(string text)
        {
            string title = NlpEngine.ExtractTaskTitle(text);
            _pendingTaskTitle = title;
            _pendingTaskDesc  = $"Task: {title}. Review and secure your cybersecurity posture.";
            _awaitingReminder = true;
            AppendBotMessage($"✅ Task noted: \"{title}\"\n\nWould you like a reminder? If so, say e.g. \"in 3 days\", \"tomorrow\", or \"no\".");
        }

        private void HandleReminderReply(string text)
        {
            _awaitingReminder = false;
            int? days = NlpEngine.ExtractReminderDays(text);
            DateTime? reminderDate = days.HasValue ? DateTime.Now.AddDays(days.Value) : null;

            int id = SaveTask(_pendingTaskTitle, _pendingTaskDesc, reminderDate);

            if (reminderDate.HasValue)
            {
                AppendBotMessage($"⏰ Got it! Task \"{_pendingTaskTitle}\" saved with a reminder on {reminderDate.Value:dd MMM yyyy}.");
                ActivityLog.Add($"Task added: '{_pendingTaskTitle}' | Reminder: {reminderDate.Value:dd MMM yyyy}");
            }
            else
            {
                AppendBotMessage($"📌 Task \"{_pendingTaskTitle}\" saved without a reminder.");
                ActivityLog.Add($"Task added: '{_pendingTaskTitle}' (no reminder).");
            }

            RefreshTaskList();
            _pendingTaskTitle = "";
            _pendingTaskDesc  = "";
        }

        private void HandleViewTasks()
        {
            var tasks = GetAllTasks();
            if (tasks.Count == 0)
            {
                AppendBotMessage("📭 You have no tasks yet. Try saying \"add task to enable 2FA\"!");
                return;
            }

            AppendBotMessage($"📋 You have {tasks.Count} task(s):");
            foreach (var t in tasks)
                AppendBotMessage($"  {t}");

            ActivityLog.Add("User viewed task list via chat.");
        }

        private void HandleCompleteTaskViaChat(string text)
        {
            int? id = NlpEngine.ExtractId(text);
            if (!id.HasValue) { AppendBotMessage("Please include the task ID, e.g. \"complete task 2\"."); return; }
            bool ok = MarkTaskDone(id.Value);
            AppendBotMessage(ok ? $"✅ Task {id} marked as done!" : $"❌ Could not find task {id}.");
            if (ok) ActivityLog.Add($"Task {id} marked as completed.");
            RefreshTaskList();
        }

        private void HandleDeleteTaskViaChat(string text)
        {
            int? id = NlpEngine.ExtractId(text);
            if (!id.HasValue) { AppendBotMessage("Please include the task ID, e.g. \"delete task 3\"."); return; }
            bool ok = DeleteTask(id.Value);
            AppendBotMessage(ok ? $"🗑️ Task {id} deleted." : $"❌ Could not find task {id}.");
            if (ok) ActivityLog.Add($"Task {id} deleted.");
            RefreshTaskList();
        }

        private void HandleSetReminderViaChat(string text)
        {
            int? days = NlpEngine.ExtractReminderDays(text);
            int? id   = NlpEngine.ExtractId(text);
            if (!days.HasValue)
            {
                AppendBotMessage("How many days? E.g. \"set reminder for task 2 in 5 days\".");
                return;
            }
            AppendBotMessage(id.HasValue
                ? $"⏰ Reminder noted for task {id} in {days} day(s). (Update via the Tasks tab.)"
                : $"⏰ Reminder set for {days} day(s) from today.");
            ActivityLog.Add($"Reminder set: {days} day(s){(id.HasValue ? $" for task {id}" : "")}.");
        }

        // ── Chat UI helpers ───────────────────────────────────────────────────

        private void AppendUserMessage(string text)
        {
            var bubble = MakeBubble(text, isBot: false);
            ChatPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private void AppendBotMessage(string text)
        {
            var bubble = MakeBubble(text, isBot: true);
            ChatPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private UIElement MakeBubble(string text, bool isBot)
        {
            var outer = new Grid { Margin = new Thickness(4, 3, 4, 3) };
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = isBot ? HorizontalAlignment.Left : HorizontalAlignment.Right
            };

            if (isBot)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "🤖",
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 6, 0)
                });
            }

            var border = new Border
            {
                Background = isBot
                    ? new SolidColorBrush(Color.FromRgb(33, 38, 45))
                    : new SolidColorBrush(Color.FromRgb(31, 111, 235)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                MaxWidth = 520
            };

            border.Child = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };

            panel.Children.Add(border);
            outer.Children.Add(panel);
            return outer;
        }

        private void ScrollToBottom()
        {
            Dispatcher.InvokeAsync(() => ChatScroll.ScrollToEnd(),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TAB 2 – TASK ASSISTANT
        // ══════════════════════════════════════════════════════════════════════

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                TaskStatus.Text = "⚠️ Please enter a task title.";
                TaskStatus.Foreground = new SolidColorBrush(Color.FromRgb(248, 81, 73));
                return;
            }

            string desc = TaskDesc.Text.Trim();
            DateTime? reminder = null;

            if (int.TryParse(ReminderDays.Text.Trim(), out int days) && days > 0)
                reminder = DateTime.Now.AddDays(days);

            int id = SaveTask(title, desc, reminder);
            string reminderInfo = reminder.HasValue ? $" | Reminder: {reminder.Value:dd MMM yyyy}" : "";
            TaskStatus.Text = $"✅ Task added (ID {id}){reminderInfo}";
            TaskStatus.Foreground = new SolidColorBrush(Color.FromRgb(63, 185, 80));

            ActivityLog.Add($"Task added via GUI: '{title}'" + (reminder.HasValue ? $" | Reminder in {days} days" : ""));

            TaskTitle.Clear(); TaskDesc.Clear(); ReminderDays.Clear();
            RefreshTaskList();
        }

        private void MarkDone_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListBox.SelectedItem is CyberTask task)
            {
                bool ok = MarkTaskDone(task.Id);
                TaskStatus.Text = ok ? $"✅ Task {task.Id} marked as done." : "❌ Update failed.";
                if (ok) ActivityLog.Add($"Task {task.Id} ('{task.Title}') marked as done.");
                RefreshTaskList();
            }
            else
            {
                TaskStatus.Text = "Select a task first.";
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListBox.SelectedItem is CyberTask task)
            {
                bool ok = DeleteTask(task.Id);
                TaskStatus.Text = ok ? $"🗑️ Task {task.Id} deleted." : "❌ Delete failed.";
                if (ok) ActivityLog.Add($"Task {task.Id} ('{task.Title}') deleted.");
                RefreshTaskList();
            }
            else
            {
                TaskStatus.Text = "Select a task first.";
            }
        }

        private void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TaskStatus.Text = "";
        }

        // ── DB / in-memory helpers ────────────────────────────────────────────

        private int SaveTask(string title, string desc, DateTime? reminder)
        {
            if (_dbAvailable)
            {
                int id = DatabaseManager.AddTask(title, desc, reminder);
                if (id > 0) return id;
            }

            // Fallback to in-memory
            var t = new CyberTask
            {
                Id          = _nextLocalId++,
                Title       = title,
                Description = desc,
                Reminder    = reminder,
                CreatedAt   = DateTime.Now
            };
            _localTasks.Add(t);
            return t.Id;
        }

        private List<CyberTask> GetAllTasks()
        {
            if (_dbAvailable)
            {
                var list = DatabaseManager.GetAllTasks();
                if (list.Count > 0 || _localTasks.Count == 0) return list;
            }
            return _localTasks.OrderByDescending(t => t.CreatedAt).ToList();
        }

        private bool MarkTaskDone(int id)
        {
            if (_dbAvailable && DatabaseManager.MarkDone(id)) return true;
            var t = _localTasks.FirstOrDefault(x => x.Id == id);
            if (t == null) return false;
            t.IsDone = true;
            return true;
        }

        private bool DeleteTask(int id)
        {
            if (_dbAvailable && DatabaseManager.DeleteTask(id)) return true;
            var t = _localTasks.FirstOrDefault(x => x.Id == id);
            if (t == null) return false;
            _localTasks.Remove(t);
            return true;
        }

        private void RefreshTaskList()
        {
            TaskListBox.Items.Clear();
            foreach (var t in GetAllTasks())
                TaskListBox.Items.Add(t);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TAB 3 – QUIZ
        // ══════════════════════════════════════════════════════════════════════

        private void StartQuiz_Click(object sender, RoutedEventArgs e) => StartQuizInternal();

        private void StartQuizInternal()
        {
            if (_quizRunning) return;

            _questions   = new List<QuizQuestion>(QuizData.Questions);
            _quizIndex   = 0;
            _quizScore   = 0;
            _quizRunning = true;

            ActivityLog.Add("Quiz started.");
            ShowQuestion();
        }

        private void ShowQuestion()
        {
            QuizPanel.Children.Clear();
            AnswerPanel.Children.Clear();

            if (_quizIndex >= _questions.Count)
            {
                EndQuiz();
                return;
            }

            var q = _questions[_quizIndex];
            QuizProgressText.Text = $"Question {_quizIndex + 1} of {_questions.Count}";
            QuizScoreText.Text    = $"Score: {_quizScore}";

            // Question card
            var card = new Border
            {
                Background    = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                CornerRadius  = new CornerRadius(10),
                Padding       = new Thickness(24),
                Margin        = new Thickness(0, 12, 0, 0)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text       = $"Q{_quizIndex + 1}: {q.Text}",
                Foreground = Brushes.White,
                FontSize   = 15,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin     = new Thickness(0, 0, 0, 16)
            });

            foreach (var opt in q.Options)
            {
                stack.Children.Add(new TextBlock
                {
                    Text       = opt,
                    Foreground = new SolidColorBrush(Color.FromRgb(201, 209, 217)),
                    FontSize   = 13,
                    Margin     = new Thickness(8, 4, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            card.Child = stack;
            QuizPanel.Children.Add(card);

            // Answer buttons
            AnswerArea.Visibility = Visibility.Visible;
            string[] labels = { "A", "B", "C", "D" };
            for (int i = 0; i < q.Options.Length && i < 4; i++)
            {
                string label = labels[i];
                var btn = new Button
                {
                    Content = label,
                    Width   = 52,
                    Height  = 36,
                    Margin  = new Thickness(6, 0, 6, 0),
                    Tag     = label
                };
                btn.Style = FindResource("CyberBtn") as Style;
                btn.Click += AnswerBtn_Click;
                AnswerPanel.Children.Add(btn);
            }

            _awaitingAnswer = true;
        }

        private void AnswerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_awaitingAnswer) return;
            _awaitingAnswer = false;

            string chosen = (sender as Button)?.Tag?.ToString() ?? "";
            var q = _questions[_quizIndex];
            bool correct = chosen.Equals(q.Answer, StringComparison.OrdinalIgnoreCase);

            if (correct) _quizScore++;

            // Feedback card
            var feedback = new Border
            {
                Background   = new SolidColorBrush(correct
                    ? Color.FromArgb(40, 63, 185, 80)
                    : Color.FromArgb(40, 248, 81, 73)),
                BorderBrush  = new SolidColorBrush(correct
                    ? Color.FromRgb(63, 185, 80)
                    : Color.FromRgb(248, 81, 73)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding      = new Thickness(16),
                Margin       = new Thickness(0, 10, 0, 0)
            };

            var fStack = new StackPanel();
            fStack.Children.Add(new TextBlock
            {
                Text       = correct ? "✅ Correct!" : $"❌ Incorrect. The answer was {q.Answer}.",
                Foreground = new SolidColorBrush(correct
                    ? Color.FromRgb(63, 185, 80)
                    : Color.FromRgb(248, 81, 73)),
                FontSize   = 14,
                FontWeight = FontWeights.Bold
            });
            fStack.Children.Add(new TextBlock
            {
                Text       = q.Explanation,
                Foreground = new SolidColorBrush(Color.FromRgb(201, 209, 217)),
                FontSize   = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin     = new Thickness(0, 6, 0, 0)
            });

            var nextBtn = new Button
            {
                Content = _quizIndex + 1 < _questions.Count ? "Next →" : "See Results",
                Margin  = new Thickness(0, 12, 0, 0),
                Width   = 120,
                Height  = 34
            };
            nextBtn.Style = FindResource("CyberBtn") as Style;
            nextBtn.Click += (_, __) =>
            {
                _quizIndex++;
                ShowQuestion();
            };

            fStack.Children.Add(nextBtn);
            feedback.Child = fStack;
            QuizPanel.Children.Add(feedback);

            // Disable answer buttons
            foreach (Button b in AnswerPanel.Children)
                b.IsEnabled = false;
        }

        private void EndQuiz()
        {
            AnswerArea.Visibility = Visibility.Collapsed;
            _quizRunning = false;

            int total   = _questions.Count;
            double pct  = (double)_quizScore / total * 100;
            string msg  = pct >= 80 ? "🏆 Great job! You're a cybersecurity pro!"
                        : pct >= 50 ? "👍 Good effort! Keep learning to stay safe online."
                        :             "📚 Keep learning – cybersecurity knowledge is your best defence!";

            ActivityLog.Add($"Quiz completed: {_quizScore}/{total} ({pct:F0}%).");
            QuizProgressText.Text = "Quiz Complete";
            QuizScoreText.Text    = $"Final: {_quizScore}/{total}";

            var resultCard = new Border
            {
                Background   = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                CornerRadius = new CornerRadius(12),
                Padding      = new Thickness(30),
                Margin       = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var rs = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            rs.Children.Add(new TextBlock
            {
                Text = $"{_quizScore} / {total}",
                Foreground = new SolidColorBrush(Color.FromRgb(227, 179, 65)),
                FontSize   = 48,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            rs.Children.Add(new TextBlock
            {
                Text = msg,
                Foreground = Brushes.White,
                FontSize   = 15,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 16)
            });

            var retry = new Button { Content = "🔄 Play Again", Width = 140, Height = 36 };
            retry.Style = FindResource("GreenBtn") as Style;
            retry.Click += (_, __) => StartQuizInternal();
            rs.Children.Add(retry);

            resultCard.Child = rs;
            QuizPanel.Children.Add(resultCard);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TAB 4 – ACTIVITY LOG
        // ══════════════════════════════════════════════════════════════════════

        private void RefreshLog_Click(object sender, RoutedEventArgs e) => RefreshLogPanel();

        private void RefreshLogPanel()
        {
            LogPanel.Children.Clear();
            var entries = ActivityLog.GetRecent(10).ToList();

            if (entries.Count == 0)
            {
                LogPanel.Children.Add(new TextBlock
                {
                    Text = "No actions recorded yet.",
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    FontSize   = 13,
                    Margin     = new Thickness(8, 12, 0, 0)
                });
                return;
            }

            foreach (var entry in entries)
            {
                var row = new Border
                {
                    Background   = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                    CornerRadius = new CornerRadius(6),
                    Padding      = new Thickness(14, 10, 14, 10),
                    Margin       = new Thickness(0, 0, 0, 6)
                };
                row.Child = new TextBlock
                {
                    Text = entry,
                    Foreground   = new SolidColorBrush(Color.FromRgb(201, 209, 217)),
                    FontSize     = 13,
                    TextWrapping = TextWrapping.Wrap
                };
                LogPanel.Children.Add(row);
            }
        }

        // ── Utility ───────────────────────────────────────────────────────────
        private static string TruncateForLog(string s, int max = 60) =>
            s.Length <= max ? s : s[..max] + "…";
    }
}
