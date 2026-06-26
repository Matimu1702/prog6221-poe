using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace CyberBot
{
    // ─────────────────────────────────────────────────────────────────────────
    // DatabaseManager  – SQL Server integration for the Task Assistant
    //
    // CONNECTION STRING NOTE (from lecturer):
    //   Change "Database=TaskChat" to match whatever database name you created
    //   in SQL Server Management Studio. Everything else can stay the same if
    //   you are using Windows Authentication on a local instance.
    // ─────────────────────────────────────────────────────────────────────────
    public class DatabaseManager
    {
        // ── Connection string ─────────────────────────────────────────────────
        // Uses Windows Authentication (Integrated Security=True) — no password needed.
        // Change "TaskChat" to your actual database name if it differs.
        private const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=TaskChat;Integrated Security=True;TrustServerCertificate=True;";

        // Alternative for a named SQL Server instance, e.g. SQL Server Express:
        // "Server=.\SQLEXPRESS;Database=TaskChat;Integrated Security=True;TrustServerCertificate=True;"

        // ── Initialise schema (called once on startup) ────────────────────────
        public static void Initialise()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                // Create tasks table if it does not already exist
                new SqlCommand(@"
                    IF NOT EXISTS (
                        SELECT * FROM sysobjects
                        WHERE name='tasks' AND xtype='U'
                    )
                    CREATE TABLE tasks (
                        id          INT IDENTITY(1,1) PRIMARY KEY,
                        title       NVARCHAR(255) NOT NULL,
                        description NVARCHAR(MAX),
                        is_done     BIT DEFAULT 0,
                        reminder    DATETIME NULL,
                        created_at  DATETIME DEFAULT GETDATE()
                    );", conn).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // If SQL Server is not available the app falls back to in-memory mode.
                Console.WriteLine("[DB] Could not connect to SQL Server: " + ex.Message);
                throw; // re-throw so caller can set _dbAvailable = false
            }
        }

        // ── Add a task ────────────────────────────────────────────────────────
        public static int AddTask(string title, string description, DateTime? reminder)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                var cmd = new SqlCommand(
                    @"INSERT INTO tasks (title, description, reminder)
                      VALUES (@t, @d, @r);
                      SELECT SCOPE_IDENTITY();",
                    conn);
                cmd.Parameters.AddWithValue("@t", title);
                cmd.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@r", reminder.HasValue ? (object)reminder.Value : DBNull.Value);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch
            {
                return -1; // DB unavailable; caller uses in-memory fallback
            }
        }

        // ── Retrieve all tasks ────────────────────────────────────────────────
        public static List<CyberTask> GetAllTasks()
        {
            var list = new List<CyberTask>();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                var reader = new SqlCommand(
                    "SELECT id, title, description, is_done, reminder, created_at FROM tasks ORDER BY created_at DESC;",
                    conn).ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new CyberTask
                    {
                        Id          = reader.GetInt32(0),
                        Title       = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        IsDone      = reader.GetBoolean(3),
                        Reminder    = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                        CreatedAt   = reader.GetDateTime(5)
                    });
                }
            }
            catch { /* return empty list if DB is down */ }
            return list;
        }

        // ── Mark task completed ───────────────────────────────────────────────
        public static bool MarkDone(int id)
        {
            return Execute("UPDATE tasks SET is_done = 1 WHERE id = @id", id);
        }

        // ── Delete task ───────────────────────────────────────────────────────
        public static bool DeleteTask(int id)
        {
            return Execute("DELETE FROM tasks WHERE id = @id", id);
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static bool Execute(string sql, int id)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CyberTask  – POCO mirroring the DB row
    // ─────────────────────────────────────────────────────────────────────────
    public class CyberTask
    {
        public int       Id          { get; set; }
        public string    Title       { get; set; } = "";
        public string    Description { get; set; } = "";
        public bool      IsDone      { get; set; }
        public DateTime? Reminder    { get; set; }
        public DateTime  CreatedAt   { get; set; }

        public override string ToString()
        {
            string status   = IsDone ? "✅" : "⏳";
            string reminder = Reminder.HasValue
                ? $" | ⏰ {Reminder.Value:dd MMM yyyy HH:mm}"
                : "";
            return $"{status} [{Id}] {Title}{reminder}";
        }
    }
}
