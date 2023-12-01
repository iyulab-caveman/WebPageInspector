using CommunityToolkit.Mvvm.ComponentModel;
using Iyu.Windows.Mvvm;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Web;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.Input;
using WebPageInspector.Services;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace WebPageInspector.Pages
{
    public sealed partial class BrowserPage : IView
    {
        public BrowserPage()
        {
            this.InitializeComponent();

            VM.WebViewService.Initialize(WebView);

            WebView.NavigationCompleted += WebView_NavigationCompleted;
        }

        public BrowserPageViewModel VM => this.ResolveViewModel<BrowserPageViewModel>();

        private async void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            try
            {
                var script = "document.documentElement.outerHTML;";
                var result = await sender.ExecuteScriptAsync(script);

                var html = System.Text.Json.JsonSerializer.Deserialize<string>(result);
                sender.Tag = html;
                this.Context = new WebPageContext(sender, html);

                UpdateMatches();
            }
            catch (Exception)
            {
            }
        }

        [ObservableProperty] private WebPageContext? context;
        [ObservableProperty] private string? expression;
        [ObservableProperty] private IEnumerable<string>? matches;

        partial void OnExpressionChanged(string? value)
        {
            UpdateMatches();
        }

        private async void UpdateMatches()
        {
            if (string.IsNullOrEmpty(Expression))
            {
                this.Matches = null;
            }
            else
            {
                try
                {
                    var regex = new Regex(Expression);
                    if (WebView.Tag is string html)
                    {
                        var r = await Task.Run(() =>
                        {
                            var text = HtmlHelper.GetText(html);
                            if (text == null) return Array.Empty<string>();

                            var matches = regex.Matches(text);
                            return matches.Select(p => p.Value);
                        });
                        this.Matches = r;
                    }
                    else
                    {
                        this.Matches = null;
                    }
                }
                catch (Exception)
                {
                    this.Matches = null;
                }
            }
        }

        [RelayCommand]
        private void NavigateLink(string link)
        {
            if (string.IsNullOrEmpty(link) != true)
            {
                var uri = link.Contains("://") ? new Uri(link) : new Uri($"https://{link}");
                WebView.Source = uri;
            }
        }

        [RelayCommand]
        private async Task BrowseLinkAsync(string link)
        {
            if (string.IsNullOrEmpty(link) != true)
            {
                var uri = link.Contains("://") ? new Uri(link) : new Uri($"https://{link}");
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }

        [RelayCommand]
        private async Task ShowImage(string link)
        {
            if (string.IsNullOrEmpty(link) != true)
            {
                var uri = link.Contains("://") ? new Uri(link) : new Uri($"https://{link}");
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }
    }
}