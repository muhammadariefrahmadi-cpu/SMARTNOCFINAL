using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using System.Text.RegularExpressions;
using SMART_NOC.Services;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using Windows.Storage;
using MsgReader.Outlook;
using SMART_NOC.Models;
using System.Collections.ObjectModel;
using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using CommunityToolkit.WinUI.Controls;

// Namespace Wajib
using SMART_NOC;

namespace SMART_NOC.Views
{
    public sealed partial class TicketFormView : UserControl
    {
        public event Action<string>? OnTitleChanged;
        public event Action<string>? OnRequestOpenMap;

        private HistoryService _historyService = new HistoryService();
        private GeminiService _geminiService = new GeminiService();
        private OcrEngine? _ocrEngine;
        private DispatcherTimer? _autoSaveTimer;
        private DispatcherTimer _debounceTimer;

        // Data Cache
        private List<string> _allSegments = new List<string>();
        private List<string> _allPics = new List<string>();
        private string? _currentImagePath;
        private bool _isDialogOpen = false;

        public ObservableCollection<ImpactItem> ImpactList { get; set; } = new ObservableCollection<ImpactItem>();
        private static Dictionary<string, bool> _suppressedWarnings = new Dictionary<string, bool>();

        private List<string> _rootCauseTemplates = new List<string>
        {
            "FO Cut due to land sliding / tanah longsor",
            "FO Cut due to vandalism / vandalisme",
            "FO Cut due to road construction / pekerjaan jalan (PU)",
            "FO Cut due to animal bite / gigitan tupai/tikus",
            "High Loss due to dirty connector / konektor kotor",
            "High Loss due to bending cable / kabel tertekuk",
            "Device Hang / Perangkat Hang perlu restart",
            "Power Loss / Mati Listrik PLN",
            "Module SFP Faulty / Modul Rusak"
        };

        public TicketFormView()
        {
            try
            {
                this.InitializeComponent();

                _debounceTimer = new DispatcherTimer();
                _debounceTimer.Interval = TimeSpan.FromMilliseconds(500);
                _debounceTimer.Tick += (s, e) =>
                {
                    _debounceTimer.Stop();
                    GeneratePreview();
                };

                // 🔥 REGISTER ACTIVITY MONITORING 🔥
                ActivityMonitor.Instance.LogNavigation("Previous", "TicketFormView/CreateTicket");

                // 🎨 APPLY ENTRANCE ANIMATIONS 🔥
                ApplyEntranceAnimations();

                // 🔥 DEFENSIVE: Initialize data with error handling 🔥
                InitData();
            }
            catch (Exception initEx)
            {
                // Don't let constructor crash
                System.Diagnostics.Debug.WriteLine($"❌ TicketFormView Constructor Error: {initEx.Message}");
                CrashLogger.Log(initEx, "TicketFormView_Constructor");
            }
        }

        // 🎨 APPLY BEAUTIFUL ENTRANCE ANIMATIONS 🎨
        private void ApplyEntranceAnimations()
        {
            try
            {
                // Animate left panel
                if (this.Content is Grid mainGrid)
                {
                    var leftPanel = mainGrid.FindName("LeftPanel") as ScrollViewer ?? mainGrid.Children.FirstOrDefault() as ScrollViewer;
                    if (leftPanel != null)
                    {
                        leftPanel.RenderTransform = new CompositeTransform();
                        var slideInLeft = AnimationHelper.CreateSlideInAnimation(leftPanel, "Left", 600, 40);
                        slideInLeft.Begin();
                    }

                    // Animate center panel
                    var centerPanel = mainGrid.FindName("CenterPanel") as ScrollViewer;
                    if (centerPanel != null)
                    {
                        centerPanel.RenderTransform = new CompositeTransform();
                        var slideInCenter = AnimationHelper.CreateSlideInAnimation(centerPanel, "Left", 700, 40);
                        slideInCenter.Begin();
                    }

                    // Animate right panel
                    var rightPanel = mainGrid.FindName("RightPanel") as Grid;
                    if (rightPanel != null)
                    {
                        rightPanel.RenderTransform = new CompositeTransform();
                        var slideInRight = AnimationHelper.CreateSlideInAnimation(rightPanel, "Right", 800, 40);
                        slideInRight.Begin();
                    }
                }

                // Apply hover animations to buttons
                AnimateButtons();
            }
            catch (Exception ex)
            {
                CrashLogger.LogInfo($"Animation error: {ex.Message}", "TicketFormView");
            }
        }

        // 🎨 ANIMATE ALL BUTTONS WITH HOVER EFFECTS 🎨
        private void AnimateButtons()
        {
            try
            {
                if (BtnAutoLog != null) AnimationHelper.ApplyButtonHoverAnimation(BtnAutoLog);
                if (BtnSaveJson != null) AnimationHelper.ApplyButtonHoverAnimation(BtnSaveJson);
                if (BtnCopyForm != null) AnimationHelper.ApplyButtonHoverAnimation(BtnCopyForm);
                if (BtnPasteForm != null) AnimationHelper.ApplyButtonHoverAnimation(BtnPasteForm);
                if (BtnCopy != null) AnimationHelper.ApplyButtonHoverAnimation(BtnCopy);
                if (BtnReset != null) AnimationHelper.ApplyButtonHoverAnimation(BtnReset);
                if (BtnOpenData != null) AnimationHelper.ApplyButtonHoverAnimation(BtnOpenData);
                if (BtnViewMap != null) AnimationHelper.ApplyButtonHoverAnimation(BtnViewMap);
                if (BtnAddImpact != null) AnimationHelper.ApplyButtonHoverAnimation(BtnAddImpact);
            }
            catch (Exception ex)
            {
                CrashLogger.LogInfo($"Button animation error: {ex.Message}", "TicketFormView");
            }
        }

        // =========================================================
        // 🔥 INIT DATA: VERSI CEPAT (NO WAITING LOOP) 🔥
        // =========================================================
        private void InitData()
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                // 🔥 DEFENSIVE: Check GlobalDatabase exists 🔥
                if (App.GlobalDatabase == null)
                {
                    AddToLog("⚠️ WARNING: GlobalDatabase not initialized!");
                    _allSegments = new List<string>();
                    _allPics = new List<string>();
                }
                else
                {
                    // KARENA ADA SPLASH SCREEN, DATA DIJAMIN SUDAH ADA.
                    try
                    {
                        _allSegments = App.GlobalDatabase.GetAllSegments() ?? new List<string>();
                        _allPics = App.GlobalDatabase.GetAllPics() ?? new List<string>();
                    }
                    catch (Exception dbEx)
                    {
                        AddToLog($"⚠️ Database access error: {dbEx.Message}");
                        _allSegments = new List<string>();
                        _allPics = new List<string>();
                    }
                }

                // Setup Dropdown Langsung (Safe checks)
                if (AsbSegmentPM != null && _allSegments.Count > 0)
                {
                    AsbSegmentPM.ItemsSource = _allSegments;
                    AsbSegmentPM.PlaceholderText = "Ketik Segment PM...";
                }
                
                if (AsbInfoPic != null && _allPics.Count > 0)
                {
                    AsbInfoPic.ItemsSource = _allPics;
                }

                // Setup UI Lainnya (with null checks)
                if (LvImpacts != null)
                {
                    LvImpacts.ItemsSource = ImpactList;
                    if (ImpactList.Count == 0) ImpactList.Add(new ImpactItem { Name = "", Status = "❌" });
                }

                // 🔥 DEFENSIVE: Try OCR but don't crash if fails 🔥
                try
                {
                    _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                    if (_ocrEngine == null)
                    {
                        AddToLog("ℹ️ OCR not available on this system");
                    }
                }
                catch (Exception ocrEx)
                {
                    AddToLog($"⚠️ OCR initialization failed: {ocrEx.Message}");
                    _ocrEngine = null; // Set null, don't crash
                }

                // 🔥 DEFENSIVE: Load settings safely 🔥
                try
                {
                    var config = LocalSettings.Load();
                    if (config != null && config.IsAutoSaveEnabled)
                    {
                        _autoSaveTimer = new DispatcherTimer();
                        _autoSaveTimer.Interval = TimeSpan.FromSeconds(30);
                        _autoSaveTimer.Tick += async (s, e) => await AutoSaveTick();
                        _autoSaveTimer.Start();
                    }
                }
                catch (Exception settingsEx)
                {
                    AddToLog($"⚠️ Settings load error: {settingsEx.Message}");
                }

                // 🔥 DEFENSIVE: Initialize UI elements safely 🔥
                if (SegStatus != null && SegStatus.Items.Count > 0)
                {
                    SegStatus.SelectedIndex = 0;
                }

                GeneratePreview();
            }
            catch (Exception ex)
            {
                AddToLog($"❌ INIT ERROR: {ex.Message}");
                CrashLogger.Log(ex, "TicketFormView_InitData");
            }
        }

        // =========================================================
        // 🔥 LOGIC UTAMA: PARSING REALTIME (ID HUNTER FIX) 🔥
        // =========================================================

        private void TxtRawInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string rawBody = TxtRawInput.Text ?? "";
            if (string.IsNullOrWhiteSpace(rawBody)) return;

            try
            {
                // 1. HEADER PARSING
                var h = Regex.Match(rawBody, @"^.*\[.*MANDAU\].*$", RegexOptions.Multiline);
                if (h.Success) TxtHeaderTT.Text = h.Value.Trim();
                else if (string.IsNullOrWhiteSpace(TxtHeaderTT.Text)) TxtHeaderTT.Text = rawBody.Split('\n')[0].Trim();

                // 2. TIKET ID
                var mIOH = Regex.Match(rawBody, @"(INC-\d{8}-\d+|INC\d+)");
                if (mIOH.Success) TxtTTIOH.Text = mIOH.Value;

                var mMDU = Regex.Match(rawBody, @"(MDU-ISAT\d+|MDU-\w+-\w+-\w+)");
                // 🔥 CHANGE: TxtTTMandau -> TxtTTMandau
                if (mMDU.Success) TxtTTMandau.Text = mMDU.Value;

                // 3. WAKTU
                var occ = Regex.Match(rawBody, @"(Occur\s*Time|Time|Waktu\s*Kejadian)\s*[:=]\s*(\d{4}[-/]\d{2}[-/]\d{2}|\d{2}[-/]\d{2}[-/]\d{4})\s+\d{2}:\d{2}", RegexOptions.IgnoreCase);
                if (occ.Success) TxtOccurTime.Text = Regex.Match(occ.Value, @"(\d{4}[-/]\d{2}[-/]\d{2}|\d{2}[-/]\d{2}[-/]\d{4})\s+\d{2}:\d{2}").Value;

                if (string.IsNullOrWhiteSpace(TxtDispatchTime.Text))
                {
                    var dates = Regex.Matches(rawBody, @"(\d{4}[-/]\d{2}[-/]\d{2}|\d{2}[-/]\d{2}[-/]\d{4})\s+\d{2}:\d{2}");
                    if (dates.Count > 1) TxtDispatchTime.Text = dates[1].Value;
                }

                // 4. ROOT CAUSE & CUT POINT
                var rc = Regex.Match(rawBody, @"Rootcause\s*[:=]\s*(.*)", RegexOptions.IgnoreCase);
                if (rc.Success) TxtRootCause.Text = rc.Groups[1].Value.Trim();

                var cpMatch = Regex.Match(rawBody, @"Cut Point\s*[:=]\s*(.*)", RegexOptions.IgnoreCase);
                if (cpMatch.Success)
                {
                    string fullCP = cpMatch.Groups[1].Value.Trim();
                    var coords = ExtractCoordinates(fullCP);
                    if (!string.IsNullOrEmpty(coords))
                    {
                        TxtLatLong.Text = coords;
                        TxtCutPointDesc.Text = fullCP.Replace(coords, "").Trim(' ', ',', '(', ')');
                    }
                    else TxtCutPointDesc.Text = fullCP;
                }

                // 5. STATUS
                if (rawBody.Contains("DOWN", StringComparison.OrdinalIgnoreCase)) SegStatus.SelectedIndex = 0;
                else if (rawBody.Contains("UP", StringComparison.OrdinalIgnoreCase)) SegStatus.SelectedIndex = 1;

                // 6. 🔥 AUTO DETECT REGION 🔥
                if (CmbRegion.SelectedIndex == -1)
                {
                    string upper = rawBody.ToUpper();
                    if (upper.Contains("CENTRAL") || upper.Contains("SEMARANG") || upper.Contains("YOGYA")) SetRegion("Central Java");
                    else if (upper.Contains("WEST JAVA") || upper.Contains("BANDUNG") || upper.Contains("CIREBON")) SetRegion("West Java");
                    else if (upper.Contains("JABO") || upper.Contains("JAKARTA") || upper.Contains("BOGOR") || upper.Contains("TANGERANG") || upper.Contains("BEKASI")) SetRegion("Jabodetabek");
                    else if (upper.Contains("KALIMANTAN") || upper.Contains("BORNEO") || upper.Contains("BALIKPAPAN") || upper.Contains("SAMARINDA") || upper.Contains("PONTIANAK")) SetRegion("Kalimantan");
                    else if (upper.Contains("SUMATRA SOUTH") || upper.Contains("PALEMBANG") || upper.Contains("LAMPUNG")) SetRegion("Sumatra South");
                    else if (upper.Contains("SUMATRA NORTH") || upper.Contains("MEDAN") || upper.Contains("PEKANBARU") || upper.Contains("ACEH") || upper.Contains("PADANG")) SetRegion("Sumatra North");
                    else if (upper.Contains("EAST JAVA") || upper.Contains("SURABAYA") || upper.Contains("MALANG")) SetRegion("East Java");
                    else if (upper.Contains("BALI") || upper.Contains("NUSRA")) SetRegion("Bali Nusra");
                    else if (upper.Contains("SULAWESI") || upper.Contains("MAKASSAR") || upper.Contains("MANADO")) SetRegion("Sulawesi");
                    else if (upper.Contains("PAPUA") || upper.Contains("MALUKU")) SetRegion("Papua Maluku");
                }

                // =========================================================
                // 🔥 TARGET SEGMENT LOOKUP (ID HUNTER MODE) 🔥
                // =========================================================

                string headerSubject = TxtHeaderTT.Text ?? "";
                NocData? bestMatch = null;

                // Regex: Cari format "DWDM... <> ..."
                var explicitRouteMatch = Regex.Match(headerSubject, @"(?:DWDM|LINK|AT)\s+(?<sideA>[^<>\[\]]+?)\s*<>\s*(?<sideB>[^<>\[\]]+?)(?:\s*\[|$)", RegexOptions.IgnoreCase);

                if (explicitRouteMatch.Success)
                {
                    string rawSideA = explicitRouteMatch.Groups["sideA"].Value.Trim();
                    string rawSideB = explicitRouteMatch.Groups["sideB"].Value.Trim();

                    // 🔥 FIX: ID HUNTER REGEX 🔥
                    string idA = Regex.Match(rawSideA, @"[A-Z0-9]{5,}").Value;
                    string idB = Regex.Match(rawSideB, @"[A-Z0-9]{5,}").Value;

                    if (!string.IsNullOrEmpty(idA) && !string.IsNullOrEmpty(idB))
                    {
                        bestMatch = App.GlobalDatabase.FindSegment($"{idA} <> {idB}");
                        if (bestMatch == null) bestMatch = App.GlobalDatabase.FindSegment($"{idA}-{idB}");
                        if (bestMatch == null) bestMatch = App.GlobalDatabase.FindSegment($"{idA} - {idB}");
                        if (bestMatch == null) bestMatch = App.GlobalDatabase.FindSegment($"{idB} <> {idA}");
                        if (bestMatch == null) bestMatch = App.GlobalDatabase.FindSegment($"{idB}-{idA}");
                    }

                    // FALLBACK
                    if (bestMatch == null)
                    {
                        bestMatch = App.GlobalDatabase.FindSegment($"{rawSideA} <> {rawSideB}");
                        if (bestMatch == null) bestMatch = App.GlobalDatabase.FindSegment(rawSideA) ?? App.GlobalDatabase.FindSegment(rawSideB);
                    }
                }

                // --- LOGIC LAMA (FALLBACK) ---
                if (bestMatch == null)
                {
                    var segMatchLong = Regex.Match(headerSubject, @"(?<seg>[A-Z0-9_\-\(\)]+\s*<>\s*[A-Z0-9_\-\(\)]+)", RegexOptions.IgnoreCase);
                    if (segMatchLong.Success) AsbSegmentPM.Text = segMatchLong.Groups["seg"].Value.Trim();

                    var siteMatches = Regex.Matches(headerSubject, @"\d{2}[A-Z]{3}\d{4}");
                    if (siteMatches.Count >= 2)
                    {
                        string id1 = siteMatches[0].Value;
                        string id2 = siteMatches[1].Value;
                        bestMatch = App.GlobalDatabase.FindSegment($"{id1} <> {id2}") ?? App.GlobalDatabase.FindSegment($"{id2} <> {id1}");
                        if (bestMatch == null) bestMatch = App.GlobalDatabase.FindSegment(id1) ?? App.GlobalDatabase.FindSegment(id2);
                    }
                    else if (siteMatches.Count == 1)
                    {
                        bestMatch = App.GlobalDatabase.FindSegment(siteMatches[0].Value);
                    }

                    if (bestMatch == null && !string.IsNullOrWhiteSpace(headerSubject) && !headerSubject.Trim().Equals("MANDAU", StringComparison.OrdinalIgnoreCase))
                        bestMatch = App.GlobalDatabase.FindSegment(headerSubject);
                }

                // 7. UPDATE UI
                if (bestMatch != null)
                {
                    AsbInfoPic.Text = $"{bestMatch.PicName} | {bestMatch.TeamSerpo} | {bestMatch.PicPhone}";

                    if (!bestMatch.SegmentRoute.Contains("NOT FOUND"))
                        AsbSegmentPM.Text = bestMatch.SegmentRoute;

                    ValidateTopology(rawBody, bestMatch);
                }
                else
                {
                    CheckNotFound(headerSubject);
                }
            }
            catch (Exception ex) { AddToLog($"PARSE ERROR: {ex.Message}"); }

            RequestPreviewUpdate();
        }

        // =========================================================
        // DROPDOWN & UI LOGIC
        // =========================================================

        private void Asb_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox asb)
            {
                if (string.IsNullOrWhiteSpace(asb.Text))
                {
                    if (asb == AsbSegmentPM) asb.ItemsSource = _allSegments;
                    else if (asb == AsbInfoPic) asb.ItemsSource = _allPics;
                }
                asb.IsSuggestionListOpen = true;
            }
        }

        private void AsbSegmentPM_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs a)
        {
            RequestPreviewUpdate();

            if (string.IsNullOrWhiteSpace(s.Text))
            {
                s.ItemsSource = _allSegments;
            }
            else
            {
                var terms = s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                s.ItemsSource = _allSegments.Where(x => terms.All(term => x.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
            }
        }

        private void AsbInfoPic_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs a)
        {
            RequestPreviewUpdate();
            if (string.IsNullOrWhiteSpace(s.Text)) s.ItemsSource = _allPics;
            else s.ItemsSource = _allPics.Where(x => x.Contains(s.Text, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void AsbSegmentPM_SuggestionChosen(AutoSuggestBox s, AutoSuggestBoxSuggestionChosenEventArgs a) { s.Text = a.SelectedItem.ToString(); RequestPreviewUpdate(); }
        private void AsbInfoPic_SuggestionChosen(AutoSuggestBox s, AutoSuggestBoxSuggestionChosenEventArgs a) { s.Text = a.SelectedItem.ToString(); RequestPreviewUpdate(); }

        private void TxtRootCause_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs a) { RequestPreviewUpdate(); if (a.Reason == AutoSuggestionBoxTextChangeReason.UserInput) s.ItemsSource = _rootCauseTemplates.Where(x => x.Contains(s.Text, StringComparison.OrdinalIgnoreCase)).ToList(); }
        private void TxtRootCause_SuggestionChosen(AutoSuggestBox s, AutoSuggestBoxSuggestionChosenEventArgs a) { s.Text = a.SelectedItem.ToString(); RequestPreviewUpdate(); }

        // Event Handlers for XAML
        private void Input_TextChanged(object sender, RoutedEventArgs e) => RequestPreviewUpdate();
        private void OnTextChange(object sender, TextChangedEventArgs e) => RequestPreviewUpdate();
        private void OnComboChange(object sender, SelectionChangedEventArgs e) => RequestPreviewUpdate();

        private void SetRegion(string keyword)
        {
            foreach (ComboBoxItem item in CmbRegion.Items)
            {
                if (item.Content.ToString().ToUpper().Contains(keyword.ToUpper()))
                {
                    CmbRegion.SelectedItem = item;
                    break;
                }
            }
        }

        // =========================================================
        // SISA KODE (DRAG DROP, SAVE, DLL)
        // =========================================================

        private void RequestPreviewUpdate()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async Task AutoSaveTick()
        {
            if (!string.IsNullOrWhiteSpace(TxtTTIOH.Text))
            {
                try { await SaveTicketAsyncLogic(isSilent: true); } catch { }
            }
        }

        private void AddToLog(string message) => MainWindow.Instance?.AddLog(message);

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Lepas untuk baca Email";
                e.DragUIOverride.IsCaptionVisible = true;
                DropZoneArea.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 0, 122, 255));
            }
            else { e.AcceptedOperation = DataPackageOperation.None; }
        }

        private async void DropZone_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                DropZoneArea.Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"];
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0 && items[0] is StorageFile file && file.FileType.ToLower() == ".msg")
                    {
                        await ReadMsgFile(file);
                    }
                    else { await ShowMessageDialog("Format Salah", "Mohon drag file Outlook (.msg)."); }
                }
            }
            catch (Exception ex) { AddToLog($"EMAIL DROP ERROR: {ex.Message}"); }
            finally { deferral.Complete(); }
        }

        private async Task ReadMsgFile(StorageFile file)
        {
            try
            {
                byte[] fileBytes = null;
                using (var memoryStream = new MemoryStream())
                {
                    bool success = false;
                    if (!string.IsNullOrEmpty(file.Path))
                    {
                        try
                        {
                            using (var fs = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                await fs.CopyToAsync(memoryStream);
                                success = true;
                            }
                        }
                        catch { }
                    }

                    if (!success)
                    {
                        using (IRandomAccessStream winStream = await file.OpenReadAsync())
                        using (Stream reader = winStream.AsStreamForRead())
                            await reader.CopyToAsync(memoryStream);
                    }
                    fileBytes = memoryStream.ToArray();
                }

                using (var ms = new MemoryStream(fileBytes))
                using (var msg = new MsgReader.Outlook.Storage.Message(ms))
                {
                    AddToLog($"Parsed Email: {msg.Subject}");
                    string subject = msg.Subject;

                    if (!string.IsNullOrWhiteSpace(subject))
                    {
                        TxtHeaderTT.Text = subject;
                    }

                    var senderMsg = msg.Sender;
                    string combined = (senderMsg?.Email ?? "").ToLower() + " " + (senderMsg?.DisplayName ?? "").ToLower();
                    if ((combined.Contains("mshfo") || combined.Contains("3pp")) && msg.SentOn.HasValue)
                        TxtDispatchTime.Text = msg.SentOn.Value.ToString("dd/MM/yyyy HH:mm");

                    TxtRawInput.Text = msg.BodyText ?? msg.BodyHtml;
                }
            }
            catch (Exception ex) { AddToLog($"MSG PARSE ERROR: {ex.Message}"); }
        }

        private void DropZoneImage_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Lepas untuk Scan Gambar";
                e.DragUIOverride.IsCaptionVisible = true;
                DropZoneImage.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 255, 149, 0));
            }
        }

        private async void DropZoneImage_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                DropZoneImage.Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"];
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0 && items[0] is StorageFile file)
                    {
                        string ext = file.FileType.ToLower();
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                        {
                            string tempPath = Path.GetTempPath();
                            string newName = $"EVID_{DateTime.Now.Ticks}{ext}";
                            string fullPath = Path.Combine(tempPath, newName);

                            using (var s = await file.OpenStreamForReadAsync())
                            using (var ms = new MemoryStream())
                            {
                                await s.CopyToAsync(ms);
                                await File.WriteAllBytesAsync(fullPath, ms.ToArray());
                            }

                            _currentImagePath = fullPath;
                            AddToLog($"Image Scanned: {file.Name}");

                            var fileForAi = await StorageFile.GetFileFromPathAsync(fullPath);
                            var config = LocalSettings.Load();
                            if (config.UseAI) await ProcessGeminiImage(fileForAi);
                            else await ProcessOfflineOcr(fileForAi);
                        }
                    }
                }
            }
            catch (Exception ex) { AddToLog($"IMAGE ERROR: {ex.Message}"); }
            finally { deferral.Complete(); }
        }

        private void TxtTTIOH_TextChanged(object sender, TextChangedEventArgs e)
        {
            RequestPreviewUpdate();
            string newTitle = string.IsNullOrWhiteSpace(TxtTTIOH.Text) ? "New Ticket" : TxtTTIOH.Text;
            OnTitleChanged?.Invoke(newTitle);
        }

        private async Task ProcessGeminiImage(StorageFile file)
        {
            var config = LocalSettings.Load();
            if (string.IsNullOrWhiteSpace(config.GeminiApiKey)) { await ShowMessageDialog("API Key Missing", "Isi Gemini API Key di Settings."); return; }

            TxtRawInput.Text += "\n\n[GEMINI AI]: Processing...";
            try
            {
                string rawResult = await _geminiService.ExtractTextFromImageAsync(file, config.GeminiApiKey);
                string jsonResult = rawResult.Replace("```json", "").Replace("```", "").Trim();

                if (jsonResult.StartsWith("{"))
                {
                    try
                    {
                        dynamic data = JsonConvert.DeserializeObject(jsonResult)!;
                        string c = data.coordinates;
                        string t = data.full_text;

                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (!string.IsNullOrEmpty(c)) { TxtLatLong.Text = c; TxtRawInput.Text += $"\nCoordinates: {c}"; }
                            if (!string.IsNullOrEmpty(t)) TxtRawInput.Text += $"\nText: {t}";
                        });
                    }
                    catch { }
                }
            }
            catch (Exception ex) { this.DispatcherQueue.TryEnqueue(() => TxtRawInput.Text += $"\nError: {ex.Message}"); }
        }

        private async Task ProcessOfflineOcr(StorageFile file)
        {
            if (_ocrEngine == null) return;
            using (IRandomAccessStream s = await file.OpenAsync(FileAccessMode.Read))
            {
                var dec = await BitmapDecoder.CreateAsync(s);
                var bmp = await dec.GetSoftwareBitmapAsync();
                var res = await _ocrEngine.RecognizeAsync(bmp);
                StringBuilder sb = new StringBuilder();
                foreach (var l in res.Lines) sb.AppendLine(l.Text);
                ProcessExtractedText(sb.ToString(), file.Name, "OCR");
            }
        }

        private void ProcessExtractedText(string text, string fname, string src)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                string c = ExtractCoordinates(text);
                if (!string.IsNullOrEmpty(c)) { TxtLatLong.Text = c; TxtRawInput.Text += $"\n[{src}]: Coords {c}"; }
                else TxtRawInput.Text += $"\n[{src}]: {text}";
            });
        }

        private string ExtractCoordinates(string text)
        {
            var regex = new Regex(@"(-?\d{1,3}[.,]\d+)\s*([NSEW]?)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);
            double? lat = null, lon = null;
            foreach (Match m in matches)
            {
                string ns = m.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(ns, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    if (m.Groups[2].Value.ToUpper() == "S" || (lat == null && val < 0)) lat = val;
                    else if (lon == null) lon = val;
                }
            }
            if (lat.HasValue && lon.HasValue) return $"{lat}, {lon}";
            return "";
        }

        private void ImpactItem_TextChanged(object sender, TextChangedEventArgs e) => RequestPreviewUpdate();
        private void ImpactItem_SelectionChanged(object sender, SelectionChangedEventArgs e) => RequestPreviewUpdate();
        private void SegStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => RequestPreviewUpdate();
        private void BtnAddImpact_Click(object sender, RoutedEventArgs e) { ImpactList.Add(new ImpactItem { Name = "", Status = "❌" }); RequestPreviewUpdate(); }
        private void BtnDeleteImpact_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.DataContext is ImpactItem i) { ImpactList.Remove(i); RequestPreviewUpdate(); } }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TxtRawInput.Text = ""; TxtHeaderTT.Text = ""; TxtTTIOH.Text = "";
            // 🔥 CHANGE: TxtTTMandau -> TxtTTMandau
            TxtTTMandau.Text = "";
            // 🔥 RESET REGION 🔥
            CmbRegion.SelectedIndex = -1;

            AsbSegmentPM.Text = ""; TxtOccurTime.Text = ""; TxtDispatchTime.Text = ""; TxtClosedTime.Text = "";
            AsbInfoPic.Text = ""; TxtCutPointDesc.Text = ""; TxtLatLong.Text = "";
            TxtRootCause.Text = ""; TxtUpdates.Text = ""; _currentImagePath = null;
            ImpactList.Clear(); ImpactList.Add(new ImpactItem { Name = "", Status = "❌" });
            SegStatus.SelectedIndex = 0;
            RequestPreviewUpdate();
            AddToLog("Form Reset.");
        }

        private async void BtnAutoLog_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDispatchTime.Text)) { await ShowMessageDialog("Info", "Dispatch Time kosong."); return; }
            DateTime dt;
            if (DateTime.TryParse(TxtDispatchTime.Text, out dt))
            {
                // 🔥 CHANGE: TxtTTMandau -> TxtTTMandau
                string log = $"{dt.AddMinutes(10):HH:mm} We have open TT {(string.IsNullOrWhiteSpace(TxtTTMandau.Text) ? "[TT]" : TxtTTMandau.Text)} (Segmen {AsbSegmentPM.Text}). Team prepare tools";
                TxtUpdates.Text += (string.IsNullOrEmpty(TxtUpdates.Text) ? "" : "\n") + log;
            }
        }

        private async void BtnSaveJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnSaveJson.Content = "Saving... ⏳";
                await SaveTicketAsyncLogic(false);
                BtnSaveJson.Content = "Auto Saved! ✅";
                await Task.Delay(1500);
                BtnSaveJson.Content = CreateIconContent("\uE74E", "Save to History");
            }
            catch (Exception ex)
            {
                BtnSaveJson.Content = "Error ❌";
                await ShowMessageDialog("Gagal Simpan", ex.Message);
            }
        }

        private void BtnOpenData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMART_NOC");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch { }
        }

        private async Task SaveTicketAsyncLogic(bool isSilent)
        {
            if (string.IsNullOrWhiteSpace(TxtTTIOH.Text)) { if (!isSilent) await ShowMessageDialog("Error", "TT IOH Wajib diisi!"); return; }

            string rawSt = "DOWN";
            if (SegStatus.SelectedItem is SegmentedItem si) rawSt = si.Tag?.ToString() ?? "DOWN";

            // 🔥 SAVE REGION 🔥
            string region = (CmbRegion.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            var t = new TicketLog
            {
                TT_IOH = TxtTTIOH.Text,
                Header = TxtHeaderTT.Text,
                TT_Mandau = TxtTTMandau.Text,
                Region = region,
                OccurTime = TxtOccurTime.Text,
                DispatchTime = TxtDispatchTime.Text,
                ClosedTime = TxtClosedTime.Text,
                SegmentPM = AsbSegmentPM.Text,
                CutPoint = FormatCutPoint(TxtCutPointDesc.Text, TxtLatLong.Text),
                RootCause = TxtRootCause.Text,
                Updates = TxtUpdates.Text,
                Status = rawSt,
                PicInfo = AsbInfoPic.Text,
                ImpactDetail = ImpactList.ToList()
            };

            await _historyService.SaveTicketAsync(t, _currentImagePath);
            
            // 🔥 TRIGGER MAP REFRESH - dengan debug logging 🔥
            if (MainWindow.Instance != null && MainWindow.Instance.MapPageInstance != null)
            {
                try
                {
                    AddToLog($"🔄 Refreshing map for new ticket: {t.TT_IOH}");
                    await MainWindow.Instance.MapPageInstance.RefreshMapData();
                    if (!isSilent) AddToLog($"✅ Saved: {t.TT_IOH} - Map updated!");
                }
                catch (Exception refreshEx)
                {
                    AddToLog($"⚠️ Saved: {t.TT_IOH} but map refresh failed: {refreshEx.Message}");
                    CrashLogger.Log(refreshEx, "MapRefreshFailed", "WARNING");
                }
            }
            else
            {
                if (!isSilent) AddToLog($"✅ Saved: {t.TT_IOH}");
                // 🔥 LOG if map instance is null 🔥
                if (MainWindow.Instance == null)
                    CrashLogger.LogInfo("MainWindow.Instance is null in SaveTicketAsyncLogic", "SaveTicket");
                else if (MainWindow.Instance.MapPageInstance == null)
                    CrashLogger.LogInfo("MapPageInstance is null - Map may not auto-refresh. User should navigate to map manually.", "SaveTicket");
            }
        }

        // 🔥 NEW METHOD: Format CutPoint dengan validasi koordinat 🔥
        private string FormatCutPoint(string description, string coordinates)
        {
            // Jika koordinat kosong atau hanya whitespace, jangan tambahkan kurung
            if (string.IsNullOrWhiteSpace(coordinates))
            {
                return description;
            }

            // Validasi bahwa koordinat ada format yang valid
            var trimmed = coordinates.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return description;
            }

            // Format: "Deskripsi (lat, lon)"
            return $"{description} ({trimmed})".Trim();
        }

        public void PopulateForm(TicketLog t)
        {
            TxtRawInput.TextChanged -= TxtRawInput_TextChanged;

            TxtHeaderTT.Text = t.Header ?? "";
            TxtTTIOH.Text = t.TT_IOH ?? "";
            // 🔥 CHANGE: TxtTTMandau -> TxtTTMandau
            TxtTTMandau.Text = t.TT_Mandau ?? "";

            // 🔥 LOAD REGION 🔥
            if (!string.IsNullOrEmpty(t.Region))
            {
                foreach (ComboBoxItem item in CmbRegion.Items)
                {
                    if (item.Content.ToString() == t.Region)
                    {
                        CmbRegion.SelectedItem = item;
                        break;
                    }
                }
            }

            TxtOccurTime.Text = t.OccurTime ?? "";
            TxtDispatchTime.Text = t.DispatchTime ?? "";
            TxtClosedTime.Text = t.ClosedTime ?? "";
            AsbSegmentPM.Text = t.SegmentPM ?? "";
            TxtRootCause.Text = t.RootCause ?? "";
            TxtUpdates.Text = t.Updates ?? "";
            AsbInfoPic.Text = t.PicInfo ?? "";

            if (!string.IsNullOrEmpty(t.CutPoint))
            {
                // 🔥 IMPROVED: Better parsing dengan validasi 🔥
                var match = Regex.Match(t.CutPoint, @"^(.*?)\s*\(([-\d.,\s]+)\)\s*$");
                if (match.Success)
                {
                    TxtCutPointDesc.Text = match.Groups[1].Value.Trim();
                    string coords = match.Groups[2].Value.Trim();
                    
                    // Validasi koordinat sebelum set
                    if (!string.IsNullOrWhiteSpace(coords))
                    {
                        TxtLatLong.Text = coords;
                    }
                }
                else
                {
                    // Jika format tidak sesuai, set seluruh string ke deskripsi
                    TxtCutPointDesc.Text = t.CutPoint;
                    TxtLatLong.Text = "";
                }
            }

            foreach (var item in SegStatus.Items)
            {
                if (item is SegmentedItem si && si.Tag?.ToString() == t.Status)
                {
                    SegStatus.SelectedItem = si;
                    break;
                }
            }

            ImpactList.Clear();
            if (t.ImpactDetail != null)
            {
                foreach (var item in t.ImpactDetail) ImpactList.Add(item);
            }
            if (ImpactList.Count == 0) ImpactList.Add(new ImpactItem { Name = "", Status = "❌" });

            _currentImagePath = t.ImagePath;

            TxtRawInput.TextChanged += TxtRawInput_TextChanged;
            GeneratePreview();
            AddToLog($"Data Loaded: {t.TT_IOH}");
        }

        private void GeneratePreview()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var i in ImpactList)
            {
                if (!string.IsNullOrWhiteSpace(i.Name))
                    sb.AppendLine($"- {i.Name} {i.Status}");
            }
            string imp = sb.Length > 0 ? $"Impact List :\n{sb}\n" : "";

            string st = "DOWN";
            if (SegStatus.SelectedItem is SegmentedItem si) st = si.Tag?.ToString() ?? "DOWN";
            string iconSt = st == "DOWN" ? "❌" : (st == "UP" ? "✅" : "⚠️");

            string head = !string.IsNullOrWhiteSpace(TxtHeaderTT.Text)
                ? $"*{TxtHeaderTT.Text}*"
                : $"*[MANDAU] LINK {iconSt} {st} AT {GetValue(AsbSegmentPM)}*";

            string cl = !string.IsNullOrWhiteSpace(TxtClosedTime.Text)
                ? $"Closed Time = {TxtClosedTime.Text}\n"
                : "";

            string cpDesc = GetValue(TxtCutPointDesc);
            string cpLL = GetValue(TxtLatLong);
            string cpFull = string.IsNullOrEmpty(cpLL) ? cpDesc : $"{cpDesc} ({cpLL})";

            // 🔥 REGION LINE ADDED 🔥
            string region = (CmbRegion.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "-";

            string tmpl =
$@"{head}

{imp}TT IOH = {GetValue(TxtTTIOH)}
Mandau TT = {GetValue(TxtTTMandau)}
Region = {region}
Occur Time = {GetValue(TxtOccurTime)}
Dispatch Time = {GetValue(TxtDispatchTime)}
{cl}Duration = {CalculateDuration()}
Segment PM = {GetValue(AsbSegmentPM)}
PIC = {GetValue(AsbInfoPic)}
Status Link = {iconSt} {st}
Rootcause = {GetValue(TxtRootCause)}
Cut Point = {cpFull}

Update Progress:
{GetValue(TxtUpdates)}";

            if (LblPreview != null) LblPreview.Text = tmpl;
        }

        private StackPanel CreateIconContent(string glyph, string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            sp.Children.Add(new FontIcon { Glyph = glyph, FontSize = 14 });
            sp.Children.Add(new TextBlock { Text = text });
            return sp;
        }

        private async void ValidateTopology(string raw, NocData fd)
        {
            var ids = Regex.Matches(raw, @"\((.*?)\)").SelectMany(m => Regex.Matches(m.Value, @"\d{5,}")).Select(m => m.Value).ToHashSet();
            bool diff = false; foreach (var id in ids) if (!fd.SegmentRoute.Contains(id)) { diff = true; break; }
            if (!string.IsNullOrEmpty(TxtHeaderTT.Text) && raw.Contains("MANDAU") && diff && !_isDialogOpen)
            {
                _isDialogOpen = true;
                await ShowWarningWithCheckbox("⚠️ Topology Warning", $"Segment DB ({fd.SegmentRoute}) beda dengan laporan ({string.Join(", ", ids)}).", "TOPOLOGY_WARNING");
                _isDialogOpen = false;
            }
        }

        private async void CheckNotFound(string raw)
        {
            if (!string.IsNullOrEmpty(TxtHeaderTT.Text) && raw.Contains("MANDAU") && !_isDialogOpen)
            {
                _isDialogOpen = true;
                await ShowWarningWithCheckbox("❌ Not Found", "Segment tidak ada di DB.", "SEGMENT_NOT_FOUND");
                _isDialogOpen = false;
            }
        }

        private async Task ShowWarningWithCheckbox(string title, string content, string warningKey)
        {
            // 🔥 DEFENSIVE: Safe warning dialog with XamlRoot check 🔥
            try
            {
                if (_suppressedWarnings.ContainsKey(warningKey) && _suppressedWarnings[warningKey]) 
                    return;
                
                // Check if we can show dialog
                if (this.Content is not FrameworkElement fe || fe.XamlRoot == null)
                {
                    AddToLog($"⚠️ Warning dialog skipped (no XamlRoot): {title}");
                    return;
                }

                StackPanel panel = new StackPanel { Spacing = 12 };
                panel.Children.Add(new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap });
                CheckBox dontShowBox = new CheckBox 
                { 
                    Content = "Don't show this warning again", 
                    FontSize = 12, 
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) 
                };
                panel.Children.Add(dontShowBox);
                
                ContentDialog d = new ContentDialog 
                { 
                    Title = title, 
                    Content = panel, 
                    CloseButtonText = "OK", 
                    XamlRoot = fe.XamlRoot 
                };
                
                await d.ShowAsync();
                
                if (dontShowBox.IsChecked == true)
                {
                    if (!_suppressedWarnings.ContainsKey(warningKey))
                        _suppressedWarnings.Add(warningKey, true);
                    else
                        _suppressedWarnings[warningKey] = true;
                }
            }
            catch (Exception dialogEx)
            {
                AddToLog($"⚠️ Warning dialog failed: {dialogEx.Message}");
            }
        }

        private async Task ShowMessageDialog(string t, string c)
        {
            // 🔥 DEFENSIVE: Safe dialog display with XamlRoot check 🔥
            try
            {
                _isDialogOpen = true;
                
                // Check if we can show dialog
                if (this.Content is not FrameworkElement fe || fe.XamlRoot == null)
                {
                    AddToLog($"⚠️ Cannot show dialog (no XamlRoot available): {t}");
                    return;
                }

                ContentDialog d = new ContentDialog
                {
                    Title = t,
                    Content = c,
                    CloseButtonText = "OK",
                    XamlRoot = fe.XamlRoot
                };
                
                await d.ShowAsync();
            }
            catch (Exception dialogEx)
            {
                AddToLog($"⚠️ Dialog failed: {dialogEx.Message}");
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private string CalculateDuration()
        {
            if (DateTime.TryParse(TxtDispatchTime.Text, out DateTime s))
            {
                DateTime e = DateTime.TryParse(TxtClosedTime.Text, out DateTime c) ? c : DateTime.Now;
                TimeSpan d = e - s;
                return $"{(int)d.TotalHours}h {d.Minutes}m";
            }
            return "-";
        }

        private string GetValue(TextBox t) => t?.Text ?? "";
        private string GetValue(AutoSuggestBox a) => a?.Text ?? "";

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage(); dp.SetText(LblPreview.Text); Clipboard.SetContent(dp);

            try { SaveTicketAsyncLogic(true); BtnCopy.Content = "Copied & Saved! ✅"; }
            catch { BtnCopy.Content = "Copied (Save Failed) ⚠️"; }

            var tm = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            tm.Tick += (s, a) =>
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                sp.Children.Add(new FontIcon { Glyph = "\uE8BD", FontSize = 16 });
                sp.Children.Add(new TextBlock { Text = "COPY TO WHATSAPP", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
                BtnCopy.Content = sp;
                tm.Stop();
            };
            tm.Start();
        }

        private async void BtnCopyForm_Click(object sender, RoutedEventArgs e)
        {
            var t = new TicketLog { TT_IOH = TxtTTIOH.Text, Header = TxtHeaderTT.Text };
            var pkg = new DataPackage();
            pkg.SetText(JsonConvert.SerializeObject(t));
            Clipboard.SetContent(pkg);

            BtnCopyForm.Content = "Copied! 👍";
            var tm = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            tm.Tick += (s, a) =>
            {
                BtnCopyForm.Content = CreateIconContent("\uE8C8", "Copy Form");
                tm.Stop();
            };
            tm.Start();
        }

        private async void BtnPasteForm_Click(object sender, RoutedEventArgs e)
        {
            var view = Clipboard.GetContent();
            if (view.Contains(StandardDataFormats.Text))
            {
                string json = await view.GetTextAsync();
                try
                {
                    var t = JsonConvert.DeserializeObject<TicketLog>(json);
                    if (t != null) PopulateForm(t);

                    BtnPasteForm.Content = "Pasted! 👌";
                    var tm = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                    tm.Tick += (s, a) =>
                    {
                        BtnPasteForm.Content = CreateIconContent("\uE77F", "Paste Form");
                        tm.Stop();
                    };
                    tm.Start();
                }
                catch { await ShowMessageDialog("Error", "Clipboard invalid."); }
            }
        }

        private void BtnViewMap_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtLatLong.Text)) OnRequestOpenMap?.Invoke(TxtLatLong.Text);
        }
    }
}