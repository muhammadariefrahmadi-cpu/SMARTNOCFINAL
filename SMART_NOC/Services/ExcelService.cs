using ClosedXML.Excel;
using ExcelDataReader;
using SMART_NOC.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

// Namespace Wajib
using SMART_NOC;

namespace SMART_NOC.Services
{
    public class ExcelService
    {
        // =========================================================
        // 1. FITUR EXPORT (UPDATED: Added Region Column)
        // =========================================================
        public async Task<bool> ExportTicketsToExcel(List<TicketLog> tickets)
        {
            try
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Excel Workbook", new List<string>() { ".xlsx" });
                savePicker.SuggestedFileName = $"NOC_Report_{DateTime.Now:yyyyMMdd_HHmm}";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file == null) return false;

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Incident History");

                    // HEADER (Urutan disesuaikan)
                    string[] headers = {
                        "Ticket ID",
                        "Region",        // 🔥 New Column
                        "Segment PM",
                        "Cut Point",
                        "Status",
                        "Dispatch Time",
                        "Closed Time",
                        "Root Cause",
                        "Action",
                        "PIC Area"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    }

                    // DATA
                    for (int i = 0; i < tickets.Count; i++)
                    {
                        var t = tickets[i];
                        worksheet.Cell(i + 2, 1).Value = t.TT_IOH;
                        worksheet.Cell(i + 2, 2).Value = t.Region;      // 🔥 New Data
                        worksheet.Cell(i + 2, 3).Value = t.SegmentPM;
                        worksheet.Cell(i + 2, 4).Value = t.CutPoint;
                        worksheet.Cell(i + 2, 5).Value = t.Status;
                        worksheet.Cell(i + 2, 6).Value = t.DispatchTime;
                        worksheet.Cell(i + 2, 7).Value = t.ClosedTime;
                        worksheet.Cell(i + 2, 8).Value = t.RootCause;
                        worksheet.Cell(i + 2, 9).Value = t.Action;
                        worksheet.Cell(i + 2, 10).Value = t.PicInfo;
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = await file.OpenStreamForWriteAsync())
                    {
                        stream.SetLength(0);
                        workbook.SaveAs(stream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                try { CrashLogger.Log(ex, "EXCEL_EXPORT"); } catch { }
                return false;
            }
        }

        // =========================================================
        // 2. FITUR IMPORT (SNIPER MODE: Tetap Presisi)
        // =========================================================
        public async Task<List<TicketLog>> ImportTicketsFromExcel()
        {
            var tickets = new List<TicketLog>();

            try
            {
                var openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".xlsx");
                openPicker.FileTypeFilter.Add(".xls");
                openPicker.FileTypeFilter.Add(".csv");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file == null) return tickets;

                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet();

                        // Loop Semua Sheet (Cari yang isinya Ticket)
                        foreach (DataTable table in result.Tables)
                        {
                            // Filter sheet kecil/kosong
                            if (table.Rows.Count < 5 || table.Columns.Count < 20) continue;

                            // Loop Baris
                            for (int i = 0; i < table.Rows.Count; i++)
                            {
                                DataRow row = table.Rows[i];

                                // Cek Kolom Y (Index 24) -> TT IOH
                                // Ini kunci "Sniper" kita biar gak salah baca header/sampah
                                string colY = GetVal(row, 24);

                                if (string.IsNullOrWhiteSpace(colY) || !colY.ToUpper().Contains("INC-")) continue;

                                // 🔥 KETEMU DATA VALID! PROSES 🔥
                                try
                                {
                                    var t = new TicketLog();

                                    // 1. TT IOH (Kolom Y / 24)
                                    t.TT_IOH = colY;

                                    // 🔥 2. REGION (Kolom F / Index 5) 🔥
                                    // A=0, B=1, C=2, D=3, E=4, F=5
                                    t.Region = GetVal(row, 5);

                                    // 3. TT Mandau (Kolom AA / 26)
                                    t.TT_Mandau = GetVal(row, 26);

                                    // 4. Segment PM (Kolom I / 8)
                                    t.SegmentPM = GetVal(row, 8);

                                    // 5. Cut Point (Kolom J / 9)
                                    string rawDesc = GetVal(row, 9);

                                    // 6. WAKTU
                                    // L=11, O=14, M=12
                                    t.OccurTime = ParseDate(GetVal(row, 11));
                                    t.DispatchTime = ParseDate(GetVal(row, 14));
                                    t.ClosedTime = ParseDate(GetVal(row, 12));

                                    // 7. STATUS (Kolom E / 4)
                                    string rawStatus = GetVal(row, 4).ToUpper();
                                    if (rawStatus.Contains("OPEN")) t.Status = "DOWN";
                                    else if (rawStatus.Contains("CLOSE")) t.Status = "UP";
                                    else if (rawStatus.Contains("CANCEL")) t.Status = "CANCEL";
                                    else t.Status = rawStatus;

                                    // 8. Root Cause (Kolom S / 18)
                                    t.RootCause = GetVal(row, 18);

                                    // 9. Action (Kolom T / 19)
                                    t.Action = GetVal(row, 19);

                                    // 10. PIC (Kolom BB / 53)
                                    t.PicInfo = GetVal(row, 53);

                                    // 11. COORDINATES (Kolom AP/41 & AQ/42)
                                    string latStr = GetVal(row, 41);
                                    string longStr = GetVal(row, 42);

                                    double rawLat = ParseCoordinate(latStr);
                                    double rawLong = ParseCoordinate(longStr);

                                    // Smart Fix (Tukar jika terbalik, logika Indonesia)
                                    var fixedCoords = SmartFixCoordinates(rawLat, rawLong);

                                    // Gabung ke CutPoint biar muncul di Peta
                                    if (fixedCoords.Latitude != 0 || fixedCoords.Longitude != 0)
                                    {
                                        if (string.IsNullOrWhiteSpace(rawDesc)) rawDesc = "Location Coordinate";
                                        rawDesc = Regex.Replace(rawDesc, @"\s*\(.*?\)", "").Trim();
                                        t.CutPoint = $"{rawDesc} ({fixedCoords.Latitude.ToString(CultureInfo.InvariantCulture)}, {fixedCoords.Longitude.ToString(CultureInfo.InvariantCulture)})";
                                    }
                                    else
                                    {
                                        t.CutPoint = rawDesc;
                                    }

                                    tickets.Add(t);
                                }
                                catch (Exception exRow)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Skipping Row {i}: {exRow.Message}");
                                }
                            }

                            // Stop kalau sudah ketemu data di sheet ini (asumsi 1 sheet utama)
                            if (tickets.Count > 0) break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try { CrashLogger.Log(ex, "EXCEL_IMPORT"); } catch { }
            }

            return tickets;
        }

        // =========================================================
        // HELPER (TIDAK ADA YANG DIUBAH - AMAN)
        // =========================================================
        private string GetVal(DataRow row, int index)
        {
            if (index >= 0 && index < row.ItemArray.Length)
            {
                var val = row[index];
                return val != null ? val.ToString().Trim() : "";
            }
            return "";
        }

        private double ParseCoordinate(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return 0;
            bool isNegative = val.Contains("-") || val.IndexOf("S", StringComparison.OrdinalIgnoreCase) >= 0 || val.IndexOf("W", StringComparison.OrdinalIgnoreCase) >= 0;
            var matches = Regex.Matches(val, @"\d+(\.\d+)?");

            if (matches.Count >= 3)
            {
                if (double.TryParse(matches[0].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) &&
                    double.TryParse(matches[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double m) &&
                    double.TryParse(matches[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double s))
                {
                    double result = d + (m / 60.0) + (s / 3600.0);
                    return isNegative ? -result : result;
                }
            }
            else if (matches.Count == 2)
            {
                if (double.TryParse(matches[0].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) &&
                    double.TryParse(matches[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double m))
                {
                    double result = d + (m / 60.0);
                    return isNegative ? -result : result;
                }
            }
            else if (matches.Count == 1)
            {
                if (double.TryParse(matches[0].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                {
                    return isNegative ? -Math.Abs(d) : d;
                }
            }
            return 0;
        }

        private (double Latitude, double Longitude) SmartFixCoordinates(double lat, double lng)
        {
            if (lat == 0 && lng == 0) return (0, 0);
            bool isLatSwapped = false;
            if (Math.Abs(lat) > 90) isLatSwapped = true;
            else if ((Math.Abs(lat) > 45) && (Math.Abs(lng) < 45)) isLatSwapped = true;
            return isLatSwapped ? (lng, lat) : (lat, lng);
        }

        private string ParseDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            if (DateTime.TryParse(raw, out DateTime dt)) return dt.ToString("dd/MM/yyyy HH:mm");
            if (double.TryParse(raw, out double oaDate))
            {
                try { return DateTime.FromOADate(oaDate).ToString("dd/MM/yyyy HH:mm"); } catch { }
            }
            return raw;
        }
    }
}