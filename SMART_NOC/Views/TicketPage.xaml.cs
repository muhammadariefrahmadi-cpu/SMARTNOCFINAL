using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using SMART_NOC.Services;
using Microsoft.UI.Xaml.Navigation; // PENTING buat OnNavigatedTo
using SMART_NOC.Models; // PENTING buat TicketLog

namespace SMART_NOC.Views
{
    public sealed partial class TicketPage : Page
    {
        public TicketPage()
        {
            try
            {
                this.InitializeComponent();
                AddNewTab();
            }
            catch (Exception ex)
            {
                // 🔥 CAPTURE CONSTRUCTOR ERRORS 🔥
                CrashLogger.Log(ex, "TicketPage_Constructor", "CRITICAL");
                throw; // Re-throw to let app handle
            }
        }

        // --- FITUR LOAD TO FORM (Navigasi Masuk) ---
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                base.OnNavigatedTo(e);

                // Cek apakah ada kiriman data tiket?
                if (e.Parameter is TicketLog ticketToLoad)
                {
                    // Buka tab baru dengan data tersebut
                    AddNewTab(ticketToLoad);
                }
            }
            catch (Exception ex)
            {
                // 🔥 CAPTURE NAVIGATION ERRORS 🔥
                CrashLogger.Log(ex, "TicketPage_OnNavigatedTo", "ERROR");
                MainWindow.Instance?.AddLog($"❌ Navigation error: {ex.Message}");
            }
        }

        private void AddNewTab(TicketLog? dataToLoad = null)
        {
            try
            {
                // 🔥 SHOW TABVIEW, HIDE EMPTY STATE 🔥
                if (EmptyStatePanel.Visibility == Visibility.Visible)
                {
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                }
                if (TicketTabView.Visibility == Visibility.Collapsed)
                {
                    TicketTabView.Visibility = Visibility.Visible;
                }

                var newTab = new TabViewItem
                {
                    Header = dataToLoad != null ? dataToLoad.TT_IOH : "New Ticket",
                    IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource { Symbol = Symbol.Document },
                    IsClosable = true
                };

                try
                {
                    var view = new TicketFormView();

                    // --- LOAD DATA JIKA ADA ---
                    if (dataToLoad != null)
                    {
                        view.PopulateForm(dataToLoad);
                    }
                    // --------------------------

                    view.OnTitleChanged += (newTitle) =>
                    {
                        this.DispatcherQueue.TryEnqueue(() => { newTab.Header = newTitle; });
                    };

                    var frame = new Frame();
                    frame.Content = view;
                    newTab.Content = frame;
                }
                catch (Exception exNav)
                {
                    // 🔥 CAPTURE TAB CREATION ERRORS 🔥
                    CrashLogger.Log(exNav, "TicketPage_TabCreation", "ERROR");
                    newTab.Content = new TextBlock 
                    { 
                        Text = $"Error: {exNav.Message}", 
                        Foreground = new SolidColorBrush(Colors.Red) 
                    };
                    MainWindow.Instance?.AddLog($"⚠️ Tab creation error: {exNav.Message}");
                }

                TicketTabView.TabItems.Add(newTab);
                TicketTabView.SelectedItem = newTab;
                
                // 🔥 LOG TAB CREATION 🔥
                MainWindow.Instance?.AddLog($"✅ New tab created: {newTab.Header}");
            }
            catch (Exception ex)
            {
                // 🔥 CAPTURE ADDNEWTAB ERRORS 🔥
                CrashLogger.Log(ex, "TicketPage_AddNewTab", "CRITICAL");
                MainWindow.Instance?.AddLog($"❌ AddNewTab error: {ex.Message}");
            }
        }

        private void TicketTabView_AddTabButtonClick(TabView sender, object args) 
        { 
            try
            {
                AddNewTab(); 
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "TicketTabView_AddTabButtonClick", "ERROR");
            }
        }

        private void BtnCreateNew_Click(object sender, RoutedEventArgs e) 
        { 
            try
            {
                AddNewTab();
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "BtnCreateNew_Click", "ERROR");
            }
        }

        private void BtnViewHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Navigate to History page via MainWindow
                if (this.Frame != null)
                {
                    this.Frame.Navigate(typeof(HistoryPage));
                }
                MainWindow.Instance?.AddLog("📂 Navigating to History page...");
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "BtnViewHistory_Click", "ERROR");
                MainWindow.Instance?.AddLog($"❌ Navigation error: {ex.Message}");
            }
        }

        private void TicketTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            try
            {
                string tabHeader = (args.Tab.Header as string) ?? "Unknown";
                
                sender.TabItems.Remove(args.Tab);
                
                // 🔥 TOGGLE VISIBILITY WHEN ALL TABS CLOSED 🔥
                if (sender.TabItems.Count == 0) 
                { 
                    TicketTabView.Visibility = Visibility.Collapsed; 
                    EmptyStatePanel.Visibility = Visibility.Visible;
                    MainWindow.Instance?.AddLog($"📪 Tab closed: {tabHeader} - Empty state shown");
                }
                else
                {
                    MainWindow.Instance?.AddLog($"✂️ Tab closed: {tabHeader}");
                }
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "TicketTabView_TabCloseRequested", "ERROR");
                MainWindow.Instance?.AddLog($"❌ Tab close error: {ex.Message}");
            }
        }
    }
}