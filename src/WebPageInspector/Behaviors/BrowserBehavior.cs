using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using WebPageInspector.Contexts;

namespace WebPageInspector.Behaviors
{
    public static class BrowserBehavior
    {
        #region UseContext

        public static readonly DependencyProperty UseContextProperty =
            DependencyProperty.RegisterAttached(
                "UseContext",
                typeof(bool),
                typeof(BrowserBehavior),
                new PropertyMetadata(false, OnChangedUseContext));

        public static void SetUseContext(WebView2 element, bool value)
        {
            element.SetValue(UseContextProperty, value);
        }

        public static bool GetUseContext(WebView2 element)
        {
            return (bool)element.GetValue(UseContextProperty);
        }

        public static readonly DependencyProperty ContextProperty =
            DependencyProperty.RegisterAttached(
                "Context",
                typeof(WebPageContext),
                typeof(BrowserBehavior),
                new PropertyMetadata(default(WebPageContext)));

        public static void SetContext(WebView2 element, WebPageContext value)
        {
            element.SetValue(ContextProperty, value);
        }

        public static WebPageContext GetContext(WebView2 element)
        {
            return (WebPageContext)element.GetValue(ContextProperty);
        }

        private static void OnChangedUseContext(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebView2 webView)
            {
                if ((bool)e.NewValue)
                {
                    webView.NavigationCompleted += WebView_NavigationCompleted;
                }
                else
                {
                    webView.NavigationCompleted -= WebView_NavigationCompleted;
                }
            }
        }

        private static async void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            var html = await sender.GetHtmlAsync();
            var context = new WebPageContext(sender, html);
            sender.SetCurrentValue(ContextProperty, context);
        }

        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached("Html", typeof(string), typeof(BrowserBehavior), new PropertyMetadata(default(string)));
        public static void SetHtml(WebView2 element, string value) { element.SetValue(HtmlProperty, value); }
        public static string GetHtml(WebView2 element) { return (string)element.GetValue(HtmlProperty); }


        #endregion
    }

    public static class WebViewExtensions
    {
        public static string? GetHtml(this WebView2 webView)
        {
            return BrowserBehavior.GetHtml(webView);
        }

        public static async Task<string> GetHtmlAsync(this WebView2 webView)
        {
            var script = "document.documentElement.outerHTML;";
            var result = await webView.ExecuteScriptAsync(script);

            var html = System.Text.Json.JsonSerializer.Deserialize<string>(result);

            if (string.IsNullOrEmpty(html))
                webView.ClearValue(BrowserBehavior.HtmlProperty);
            else
                webView.SetCurrentValue(BrowserBehavior.HtmlProperty, html);

            return html ?? string.Empty;
        }
    }
}
