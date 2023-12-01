using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Iyu.Windows.Mvvm;
using Microsoft.Web.WebView2.Core;
using WebPageInspector.Services;

namespace WebPageInspector.Pages
{
    public partial class BrowserPageViewModel : PageViewModel
    {
        [ObservableProperty]
        private Uri source = new("https://www.google.com");

        [ObservableProperty]
        private bool isLoading = true;

        [ObservableProperty]
        private bool hasFailures;

        public BrowserPageViewModel(IWebViewService webViewService)
        {
            WebViewService = webViewService;
        }

        public IWebViewService WebViewService
        {
            get;
        }

        [RelayCommand]
        private async Task OpenInBrowser()
        {
            if (WebViewService.Source != null)
            {
                await Windows.System.Launcher.LaunchUriAsync(WebViewService.Source);
            }
        }

        [RelayCommand]
        private void Reload()
        {
            WebViewService.Reload();
        }

        [RelayCommand(CanExecute = nameof(BrowserCanGoForward))]
        private void BrowserForward()
        {
            if (WebViewService.CanGoForward)
            {
                WebViewService.GoForward();
            }
        }

        private bool BrowserCanGoForward()
        {
            return WebViewService.CanGoForward;
        }

        [RelayCommand(CanExecute = nameof(BrowserCanGoBack))]
        private void BrowserBack()
        {
            if (WebViewService.CanGoBack)
            {
                WebViewService.GoBack();
            }
        }

        private bool BrowserCanGoBack()
        {
            return WebViewService.CanGoBack;
        }


        public override void OnNavigatedTo(object parameter)
        {
            WebViewService.NavigationCompleted += OnNavigationCompleted;
        }

        public override void OnNavigatedFrom(object parameter)
        {
            WebViewService.UnregisterEvents();
            WebViewService.NavigationCompleted -= OnNavigationCompleted;
        }

        private void OnNavigationCompleted(object? sender, CoreWebView2WebErrorStatus webErrorStatus)
        {
            IsLoading = false;
            BrowserBackCommand.NotifyCanExecuteChanged();
            BrowserForwardCommand.NotifyCanExecuteChanged();

            if (webErrorStatus != default)
            {
                HasFailures = true;
            }
        }

        [RelayCommand]
        private void OnRetry()
        {
            HasFailures = false;
            IsLoading = true;
            WebViewService?.Reload();
        }
    }
}