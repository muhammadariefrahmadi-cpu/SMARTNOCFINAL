using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using SMART_NOC.Models;
using ExcelDataReader;
using System.Data;

namespace SMART_NOC.Services
{
    public class NocDatabase
    {
        private List<NocData> _cache = new List<NocData>();
        private bool _isLoaded = false;

        public void LoadData(string filePath)
        {
            if (_isLoaded) return;
            if (!File.Exists(filePath)) return;

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet();

                        // FIX WARNING: Bikin Nullable
                        DataTable? table = null;

                        // 1. CARI SHEET "FOA Active"
                        foreach (DataTable t in result.Tables)
                        {
                            // FIX WARNING: Cek t != null
                            if (t != null && t.TableName.Trim().Equals("FOA Active", StringComparison.OrdinalIgnoreCase))
                            {
                                table = t;
                                break;
                            }
                        }

                        // Fallback: Cari berdasarkan Header Kolom
                        if (table == null)
                        {
                            foreach (DataTable t in result.Tables)
                            {
                                if (t != null && t.Rows.Count > 0)
                                {
                                    // FIX WARNING: Null Coalescing (?? "") biar gak crash
                                    var headerA = t.Rows[0][0]?.ToString()?.ToUpper() ?? "";

                                    // Cek jumlah kolom dulu sebelum akses index 9
                                    var headerJ = (t.Columns.Count > 9) ? (t.Rows[0][9]?.ToString()?.ToUpper() ?? "") : "";

                                    if (headerA.Contains("SEGMENT") || headerJ.Contains("SPAN"))
                                    {
                                        table = t;
                                        break;
                                    }
                                }
                            }
                        }

                        if (table == null) throw new Exception("Sheet 'FOA Active' tidak ditemukan di Excel.");

                        int colCount = table.Columns.Count;

                        // Helper Aman Null
                        string GetSafeValue(DataRow r, int index)
                        {
                            if (index < colCount && r[index] != null)
                                return r[index].ToString()?.Trim() ?? ""; // FIX WARNING
                            return "";
                        }

                        // 2. MULAI BACA DATA (Skip Header Row 0)
                        for (int i = 1; i < table.Rows.Count; i++)
                        {
                            var row = table.Rows[i];

                            var data = new NocData
                            {
                                SegmentRoute = GetSafeValue(row, 0), // Kolom A
                                TeamSerpo = GetSafeValue(row, 1), // Kolom B
                                PicName = GetSafeValue(row, 2), // Kolom C
                                PicPhone = GetSafeValue(row, 3), // Kolom D
                                SpanId = GetSafeValue(row, 9)  // Kolom J
                            };

                            bool isGarbage = data.PicName.Contains("Not Responsible", StringComparison.OrdinalIgnoreCase) ||
                                             data.SegmentRoute.Equals("Segment Route", StringComparison.OrdinalIgnoreCase);

                            if (!string.IsNullOrWhiteSpace(data.SegmentRoute) && !isGarbage)
                            {
                                _cache.Add(data);
                            }
                        }
                    }
                }
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Database Error: {ex.Message}");
            }
        }

        // FIX WARNING: Return type jadi NocData? (bisa null)
        public NocData? FindSegment(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText) || _cache.Count == 0) return null;

            var tokens = Regex.Matches(rawText, @"[a-zA-Z0-9]{4,}")
                              .Select(m => m.Value.ToUpper())
                              .Distinct()
                              .ToList();

            NocData? bestMatch = null;
            int maxScore = 0;

            foreach (var item in _cache)
            {
                int score = 0;
                string displayRoute = item.SegmentRoute.ToUpper();
                string logicId = item.SpanId.ToUpper();

                foreach (var token in tokens)
                {
                    if (!string.IsNullOrEmpty(logicId) && logicId.Contains(token)) score += 5;
                    else if (displayRoute.Contains(token)) score += 2;
                }

                if (score > maxScore)
                {
                    maxScore = score;
                    bestMatch = item;
                }
            }

            return maxScore > 0 ? bestMatch : null;
        }

        public List<string> GetAllSegments()
        {
            if (!_isLoaded) return new List<string>();
            return _cache.Select(x => x.SegmentRoute).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
        }

        public List<string> GetAllPics()
        {
            if (!_isLoaded) return new List<string>();
            return _cache.Select(x => x.FullPicInfo).Where(x => !string.IsNullOrWhiteSpace(x) && x.Length > 10).Distinct().OrderBy(x => x).ToList();
        }
    }
}