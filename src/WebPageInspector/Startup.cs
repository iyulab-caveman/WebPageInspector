using Iyu.Windows;
using Iyu.Windows.Contexts;
using Iyu.Windows.Navigation;
using Iyu.Windows.NavigationApps;
using Iyu.Windows.Pages;
using Iyu.Windows.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WebPageInspector.Pages;
using WebPageInspector.Services;

namespace WebPageInspector
{
    public abstract class StartupBase : NavigationApp<AppRegister>
    {
    }

    public class AppRegister : NavigationAppRegister<
        ApplicationInfo,
        AppShell,
        AppCommands,
        AppNavigationService>
    {
        public override void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            base.ConfigureServices(configuration, services);

            services.AddSingleton<MEFService>();
            services.AddSingleton<IWebViewService, WebViewService>();
            services.AddSingleton<WebCrawlingTaskService>();
        }
    }

    public class ApplicationInfo : ApplicationInfoBase
    {
    }

    public class AppShell : NavigationAppShell
    {

    }

    public class AppCommands : NavigationAppCommands
    {
    }

    public class AppNavigationService : NavigationService
    {
        public AppNavigationService()
        {
            this.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
        }

        protected override Type SettingsPageType => typeof(MefSettingsPage);

        protected override IEnumerable<MenuItemContext> InitMenuItemsSource()
        {
            yield return new MenuItemContext()
            {
                Name = "Web Browser",
                Icon = new FontIcon() { FontFamily = ResourceHelper.GetFont("SymbolThemeFontFamily"), Glyph = "\uE774" },
                PageType = typeof(BrowserPage)
            };
            yield return new MenuItemContext()
            {
                Name = "Web Crawler",
                Icon = new PathIcon() { Data = XamlHelper.Parse<Geometry>("M426.641,282.818l85.355-49.028l-10.362-20.043l-77.836,44.694l-61.896-9.506   c0.138-5.487-0.137-10.92-0.794-16.261c13.586-0.948,26.332-6.213,36.082-15.963c11.951-11.951,17.163-28.388,15.94-45.344   c36.878-11.462,37.917-37.626,37.886-50.724c-0.046-14.252-13.265-9.384-14.672-5.594c-8.344,21.075-23.811,23.91-36.113,19.715   c-2.965-4.432-6.404-8.673-10.362-12.632c-3.958-3.951-8.207-7.397-12.639-10.369c-4.187-12.295-1.36-27.761,19.73-36.113   c3.775-1.398,8.65-14.618-5.609-14.657c-13.097-0.038-39.262,1.002-50.724,37.871c-16.949-1.207-33.394,3.996-45.344,15.94   c-9.75,9.758-15.008,22.504-15.971,36.091c-5.334-0.665-10.759-0.932-16.245-0.802l-9.506-61.888l44.688-77.844L278.214,0   l-49.028,85.371l10.408,67.741c-10.683,2.453-21.274,6.396-31.453,11.798l-36.846-28.831l-95.427-23.566l-7.305,22.718   l90.964,22.474l27.234,21.312c-4.02,3.218-7.947,6.672-11.691,10.424c-5.242,5.242-9.949,10.804-14.152,16.567l-72.136-4.035   L0.003,249.7l9.736,20.403l82.589-44.412l55.079,3.079c-19.516,41.944-14.901,89.688,15.619,120.208   c30.52,30.513,78.264,35.128,120.2,15.612l3.088,55.079l-44.412,82.597L262.304,512l47.728-88.778l-4.034-72.135   c5.761-4.211,11.309-8.91,16.567-14.152c3.744-3.752,7.198-7.672,10.422-11.692l21.305,27.22l22.481,90.979l22.711-7.306   l-23.567-95.434l-28.823-36.839c5.394-10.179,9.337-20.778,11.798-31.452L426.641,282.818z") },
                PageType = typeof(WebCrawlerPage)
            };
            yield return new MenuItemContext()
            {
                Name = "Web Scraper",
                Icon = new PathIcon() { Data = XamlHelper.Parse<Geometry>("M16.829 19.233l1.027 1.53c-2.26 1.184-4.799 1.602-6.851.961l6.709-13.886c1.049-2.148 2.947-1.474 3.949-3.521.207-.423.305-.872.305-1.314.001-1.681-1.365-3.003-2.994-3.003-1.734 0-2.974 1.445-2.974 3.055 0 1.274.716 2.272-.082 3.903l-6.709 13.889c-1.762-1.229-3-3.596-3.449-6.104l1.848-.146-3.134-4.542-2.474 4.924 1.746-.156c.602 3.721 2.521 6.808 5.482 8.258 1.264.619 2.62.919 4.016.919 1.877 0 3.828-.542 5.724-1.577l.995 1.474 2.037-5.049-5.171.385zm2.171-17.234c.547 0 1 .444 1 1.001 0 .54-.441.999-1.004.999-.551 0-.996-.449-.996-1.002 0-.547.443-.998 1-.998z") },
                PageType = typeof(WebScraperPage)
            };
        }
    }
}
