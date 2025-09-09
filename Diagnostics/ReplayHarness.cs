using System;
using System.IO;
using System.Text;
using Microsoft.Maui.Storage;

namespace YasGMP.Diagnostics
{
    public static class ReplayHarness
    {
        public static string SaveReplayToken(string sessionId)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{DateTime.UtcNow:O}|{sessionId}|{Guid.NewGuid():N}"));
            try
            {
                var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, "replay_tokens.txt");
                File.AppendAllText(file, token + Environment.NewLine);
            }
            catch { }
            return token;
        }
    }
}

