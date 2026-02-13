using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SMART_NOC.Services;
using System;

namespace SMART_NOC.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            this.InitializeComponent();
            TicketStoreService.TicketsChanged += TicketStoreService_TicketsChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RefreshHistory();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            TicketStoreService.TicketsChanged -= TicketStoreService_TicketsChanged;
        }

        private void TicketStoreService_TicketsChanged(object? sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            HistoryListView.ItemsSource = TicketStoreService.GetAll();
        }
    }
}
