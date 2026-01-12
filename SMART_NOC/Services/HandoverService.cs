using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SMART_NOC.Models;

namespace SMART_NOC.Services
{
    public class HandoverService
    {
        private readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMART_NOC", "handover_logs.json");

        public async Task<List<HandoverLog>> GetLogsAsync()
        {
            if (!File.Exists(_filePath)) return new List<HandoverLog>();
            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<List<HandoverLog>>(json) ?? new List<HandoverLog>();
            }
            catch { return new List<HandoverLog>(); }
        }

        public async Task AddLogAsync(string message, bool isImportant)
        {
            var logs = await GetLogsAsync();
            logs.Insert(0, new HandoverLog { Message = message, IsImportant = isImportant });

            if (logs.Count > 100) logs = logs.GetRange(0, 100);

            string json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });

            // FIX: Cek folder null
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}