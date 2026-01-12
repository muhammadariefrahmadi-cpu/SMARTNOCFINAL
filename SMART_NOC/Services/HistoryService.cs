using Newtonsoft.Json;
using SMART_NOC.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace SMART_NOC.Services
{
    public class HistoryService
    {
        private const string JSON_FILE = "ticket_history.json";
        private const string DATA_FOLDER_NAME = "SavedTicket";

        private string GetRootFolder()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string targetFolder = Path.Combine(appData, "SMART_NOC", DATA_FOLDER_NAME);
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
                return targetFolder;
            }
            catch (Exception ex)
            {
                // 🔥 LOG FOLDER ERROR 🔥
                CrashLogger.Log(ex, "HistoryService_GetRootFolder", "CRITICAL");
                throw;
            }
        }

        private string GetJsonPath() => Path.Combine(GetRootFolder(), JSON_FILE);

        // --- GET ALL ---
        public async Task<List<TicketLog>> GetAllTicketsAsync()
        {
            try
            {
                return await LoadListInternalAsync();
            }
            catch (Exception ex)
            {
                // 🔥 LOG GET ALL ERROR 🔥
                CrashLogger.Log(ex, "HistoryService_GetAllTickets", "CRITICAL");
                throw;
            }
        }

        // --- ADD ---
        public async Task SaveTicketAsync(TicketLog ticket, string? tempImagePath)
        {
            try
            {
                var list = await LoadListInternalAsync();

                // Handle Image (Copy to local folder)
                if (!string.IsNullOrEmpty(tempImagePath) && File.Exists(tempImagePath))
                {
                    if (!tempImagePath.StartsWith(GetRootFolder()))
                    {
                        try
                        {
                            string ext = Path.GetExtension(tempImagePath);
                            string safeName = SanitizeFileName(ticket.TT_IOH);
                            if (string.IsNullOrWhiteSpace(safeName)) safeName = $"IMG_{DateTime.Now.Ticks}";
                            string fileName = $"{safeName}{ext}";
                            string destPath = Path.Combine(GetRootFolder(), fileName);

                            File.Copy(tempImagePath, destPath, true);
                            ticket.ImagePath = destPath;
                        }
                        catch (Exception imgEx)
                        {
                            // 🔥 LOG IMAGE COPY ERROR 🔥
                            CrashLogger.Log(imgEx, $"HistoryService_SaveImage_{ticket.TT_IOH}", "ERROR");
                            // Don't throw - image copy failure is not critical
                        }
                    }
                    else
                    {
                        ticket.ImagePath = tempImagePath;
                    }
                }

                // Remove existing if any (Update logic)
                var existing = list.Find(x => x.TT_IOH == ticket.TT_IOH);
                if (existing != null)
                {
                    if (string.IsNullOrEmpty(ticket.ImagePath) && !string.IsNullOrEmpty(existing.ImagePath))
                    {
                        ticket.ImagePath = existing.ImagePath;
                    }
                    list.Remove(existing);
                }

                list.Insert(0, ticket);
                await SaveListInternalAsync(list);
                
                CrashLogger.LogInfo($"Ticket saved: {ticket.TT_IOH}", "HistoryService_SaveTicket");
            }
            catch (Exception ex)
            {
                // 🔥 LOG SAVE ERROR 🔥
                CrashLogger.Log(ex, $"HistoryService_SaveTicket_{ticket?.TT_IOH}", "CRITICAL");
                throw;
            }
        }

        public async Task AddTicketAsync(TicketLog ticket)
        {
            try
            {
                // Helper wrapper for simple add (used by Import)
                await SaveTicketAsync(ticket, ticket.ImagePath);
            }
            catch (Exception ex)
            {
                // 🔥 LOG ADD ERROR 🔥
                CrashLogger.Log(ex, $"HistoryService_AddTicket_{ticket?.TT_IOH}", "CRITICAL");
                throw;
            }
        }

        // --- DELETE (INI YANG DICARI) ---
        public async Task DeleteTicketAsync(string ticketId)
        {
            try
            {
                var list = await LoadListInternalAsync();
                var item = list.FirstOrDefault(x => x.TT_IOH == ticketId);

                if (item != null)
                {
                    // Optional: Delete image file too
                    if (!string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath))
                    {
                        try
                        {
                            File.Delete(item.ImagePath);
                            CrashLogger.LogInfo($"Image deleted: {item.ImagePath}", "HistoryService_DeleteImage");
                        }
                        catch (Exception imgDelEx)
                        {
                            // 🔥 LOG IMAGE DELETE ERROR 🔥
                            CrashLogger.Log(imgDelEx, $"HistoryService_DeleteImage_{ticketId}", "ERROR");
                            // Don't throw - image deletion failure is not critical
                        }
                    }

                    list.Remove(item);
                    await SaveListInternalAsync(list);
                    
                    CrashLogger.LogInfo($"Ticket deleted: {ticketId}", "HistoryService_DeleteTicket");
                }
                else
                {
                    CrashLogger.LogInfo($"Ticket not found for deletion: {ticketId}", "HistoryService_DeleteTicket");
                }
            }
            catch (Exception ex)
            {
                // 🔥 LOG DELETE ERROR 🔥
                CrashLogger.Log(ex, $"HistoryService_DeleteTicket_{ticketId}", "CRITICAL");
                throw;
            }
        }

        // --- INTERNAL HELPERS ---
        private async Task<List<TicketLog>> LoadListInternalAsync()
        {
            try
            {
                string path = GetJsonPath();
                if (!File.Exists(path))
                {
                    CrashLogger.LogInfo($"JSON file not found, returning empty list: {path}", "HistoryService_LoadList");
                    return new List<TicketLog>();
                }

                return await Task.Run(() =>
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        var result = JsonConvert.DeserializeObject<List<TicketLog>>(json) ?? new List<TicketLog>();
                        CrashLogger.LogInfo($"Loaded {result.Count} tickets from JSON", "HistoryService_LoadList");
                        return result;
                    }
                    catch (Exception parseEx)
                    {
                        // 🔥 LOG JSON PARSE ERROR 🔥
                        CrashLogger.Log(parseEx, "HistoryService_ParseJSON", "ERROR");
                        return new List<TicketLog>();
                    }
                });
            }
            catch (Exception ex)
            {
                // 🔥 LOG LOAD ERROR 🔥
                CrashLogger.Log(ex, "HistoryService_LoadListInternal", "CRITICAL");
                throw;
            }
        }

        private async Task SaveListInternalAsync(List<TicketLog> list)
        {
            try
            {
                string path = GetJsonPath();
                await Task.Run(() =>
                {
                    try
                    {
                        string json = JsonConvert.SerializeObject(list, Formatting.Indented);
                        File.WriteAllText(path, json);
                        CrashLogger.LogInfo($"Saved {list.Count} tickets to JSON", "HistoryService_SaveList");
                    }
                    catch (Exception writeEx)
                    {
                        // 🔥 LOG WRITE ERROR 🔥
                        CrashLogger.Log(writeEx, "HistoryService_WriteJSON", "CRITICAL");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                // 🔥 LOG SAVE ERROR 🔥
                CrashLogger.Log(ex, "HistoryService_SaveListInternal", "CRITICAL");
                throw;
            }
        }

        private string SanitizeFileName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return "";
                string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
                string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
                return Regex.Replace(name, invalidRegStr, "_");
            }
            catch (Exception ex)
            {
                // 🔥 LOG SANITIZE ERROR 🔥
                CrashLogger.Log(ex, "HistoryService_SanitizeFileName", "ERROR");
                return $"FILE_{DateTime.Now.Ticks}";
            }
        }
    }
}