using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;

namespace YasGMP.Diagnostics.LogSinks
{
    internal sealed class SQLiteLogSink : ILogSink
    {
        public string Name => "sqlite";

        private readonly object _sync = new();
        private readonly string _dbPath;

        public SQLiteLogSink()
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
            Directory.CreateDirectory(dir);
            _dbPath = Path.Combine(dir, "diag.db");
            EnsureSchema();
        }

        public void WriteBatch(IReadOnlyList<DiagnosticEvent> batch)
        {
            if (batch == null || batch.Count == 0) return;
            lock (_sync)
            {
                using var con = new SqliteConnection($"Data Source={_dbPath}");
                con.Open();
                using var tx = con.BeginTransaction();
                using var cmd = con.CreateCommand();
                cmd.CommandText = "INSERT INTO logs(ts_utc, lvl, cat, evt, msg, json) VALUES ($ts, $lvl, $cat, $evt, $msg, $json)";
                var pTs = cmd.CreateParameter(); pTs.ParameterName = "$ts"; cmd.Parameters.Add(pTs);
                var pLvl = cmd.CreateParameter(); pLvl.ParameterName = "$lvl"; cmd.Parameters.Add(pLvl);
                var pCat = cmd.CreateParameter(); pCat.ParameterName = "$cat"; cmd.Parameters.Add(pCat);
                var pEvt = cmd.CreateParameter(); pEvt.ParameterName = "$evt"; cmd.Parameters.Add(pEvt);
                var pMsg = cmd.CreateParameter(); pMsg.ParameterName = "$msg"; cmd.Parameters.Add(pMsg);
                var pJson= cmd.CreateParameter(); pJson.ParameterName = "$json"; cmd.Parameters.Add(pJson);
                foreach (var e in batch)
                {
                    pTs.Value = e.TsUtc.UtcDateTime;
                    pLvl.Value = e.Level.ToString();
                    pCat.Value = e.Category;
                    pEvt.Value = e.Event;
                    pMsg.Value = e.Message;
                    pJson.Value = e.ToJson();
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        private void EnsureSchema()
        {
            try
            {
                using var con = new SqliteConnection($"Data Source={_dbPath}");
                con.Open();
                using var cmd = con.CreateCommand();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts_utc TEXT NOT NULL,
                    lvl TEXT NOT NULL,
                    cat TEXT NOT NULL,
                    evt TEXT NOT NULL,
                    msg TEXT,
                    json TEXT NOT NULL
                );";
                cmd.ExecuteNonQuery();
            }
            catch { }
        }
    }
}

