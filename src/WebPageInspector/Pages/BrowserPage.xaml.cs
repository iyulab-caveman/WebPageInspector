using Iyu.Windows.Mvvm;
using CommunityToolkit.Mvvm.Input;

namespace WebPageInspector.Pages
{
    public sealed partial class BrowserPage : IView
    {
        public BrowserPage()
        {
            this.InitializeComponent();

            VM.WebViewService.Initialize(WebView);
        }

        public BrowserPageViewModel VM => this.ResolveViewModel<BrowserPageViewModel>();

        [RelayCommand]
        private void NavigateLink(string link)
        {
            if (string.IsNullOrEmpty(link) != true)
            {
                var uri = link.Contains("://") ? new Uri(link) : new Uri($"https://{link}");
                WebView.Source = uri;
            }
        }
    }
}