//using Microsoft.Web.WebView2.Core;

//namespace WebPageInspector.Services
//{
//    public class Crawler
//    {
//        public Crawler()
//        {
//            InitBrowser();
//        }

//        private async void InitBrowser()
//        {
//            var userDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\WebPageInspector";
//            var options = new CoreWebView2EnvironmentOptions("--no-sandbox");
//            var webViewEnvironment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

//            var HWND_MESSAGE = new IntPtr(-3);
//            var webView = await webViewEnvironment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE);

//            webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            
//            webView.CoreWebView2.Navigate("https://www.google.com");
//        }

//        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
//        {

//        }
//    }
//}
