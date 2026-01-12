using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMART_NOC.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SMART_NOC.Services
{
    public class PdfService
    {
        public PdfService()
        {
            // WAJIB: Set Lisensi Community (Gratis & Legal untuk internal)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<string> GenerateBapsAsync(TicketLog ticket)
        {
            try
            {
                // 1. Tentukan Nama & Lokasi File Output
                string safeTicketId = ticket.TT_IOH.Replace("/", "_").Replace(":", "_");
                if (string.IsNullOrEmpty(safeTicketId)) safeTicketId = "DRAFT_TICKET";

                string fileName = $"BAPS_{safeTicketId}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SMART_NOC_Reports");

                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                string fullPath = Path.Combine(folderPath, fileName);

                // 2. Mulai Desain Dokumen PDF
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Setting Kertas A4
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.SegoeUI));

                        // --- A. HEADER ---
                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("BERITA ACARA PEMULIHAN SISTEM").FontSize(16).SemiBold().FontColor(Colors.Blue.Medium);
                                col.Item().Text("NETWORK OPERATION CENTER REPORT").FontSize(10).FontColor(Colors.Grey.Medium);
                            });

                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                col.Item().Text($"Date: {DateTime.Now:dd MMM yyyy}");
                                col.Item().Text($"Time: {DateTime.Now:HH:mm} WIB");
                            });
                        });

                        // --- B. ISI KONTEN ---
                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                        {
                            // 1. DATA TIKET (TABEL)
                            col.Item().Text("A. INFORMASI GANGGUAN").Bold().Underline();
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(140);
                                    columns.RelativeColumn();
                                });

                                void AddRow(string label, string value)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(label).Bold();
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(value ?? "-");
                                }

                                AddRow("Nomor Tiket (IOH)", ticket.TT_IOH);
                                AddRow("Nomor Tiket (Mandau)", ticket.TT_Mandau);
                                AddRow("Segment / Link", ticket.SegmentPM);
                                AddRow("Status Terakhir", ticket.Status);
                                AddRow("Waktu Kejadian (Down)", ticket.OccurTime);
                                AddRow("Waktu Dispatch", ticket.DispatchTime);
                                AddRow("Waktu Normal (Up)", ticket.ClosedTime);
                                AddRow("Total Durasi", CalculateDuration(ticket));
                                AddRow("PIC Lapangan", ticket.PicInfo);
                                AddRow("Titik Putus (Cut Point)", ticket.CutPoint);
                            });

                            // 2. ROOT CAUSE
                            col.Item().PaddingTop(20).Text("B. ROOT CAUSE / PENYEBAB UTAMA").Bold().Underline();
                            col.Item().PaddingTop(5).Background(Colors.Grey.Lighten4).Padding(10).Text(ticket.RootCause ?? "Belum ada analisa root cause.");

                            // 3. KRONOLOGIS UPDATE
                            col.Item().PaddingTop(20).Text("C. KRONOLOGIS PENANGANAN").Bold().Underline();
                            col.Item().PaddingTop(5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Text(ticket.Updates ?? "-").FontSize(10);

                            // 4. FOTO BUKTI (FIXED SYNTAX FOR QUESTPDF 2025)
                            if (!string.IsNullOrEmpty(ticket.ImagePath) && File.Exists(ticket.ImagePath))
                            {
                                col.Item().PaddingTop(20).Text("D. DOKUMENTASI / EVIDEN").Bold().Underline();

                                // UPDATE SYNTAX: Pake chaining .FitArea() 
                                col.Item().PaddingTop(10).AlignCenter().Height(250).Image(ticket.ImagePath).FitArea();

                                col.Item().AlignCenter().Text("Dokumentasi Lapangan").FontSize(9).Italic();
                            }
                        });

                        // --- C. FOOTER ---
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Generated by SMART NOC Commander - ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                })
                .GeneratePdf(fullPath);

                return fullPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal generate PDF: {ex.Message}");
            }
        }

        private string CalculateDuration(TicketLog t)
        {
            if (DateTime.TryParse(t.DispatchTime, out DateTime start))
            {
                DateTime end = DateTime.Now;
                if (!string.IsNullOrEmpty(t.ClosedTime) && DateTime.TryParse(t.ClosedTime, out DateTime c))
                    end = c;

                TimeSpan d = end - start;
                return $"{(int)d.TotalHours} Jam {d.Minutes} Menit";
            }
            return "-";
        }
    }
}