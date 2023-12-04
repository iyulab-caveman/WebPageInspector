using CommunityToolkit.Mvvm.ComponentModel;
using Iyu.Windows.Mvvm;
using System.Text.Json;

namespace WebPageInspector.Pages
{
    public sealed partial class WebCrawlerPage : IView
    {
        public WebCrawlerPage()
        {
            this.InitializeComponent();
        }

        [ObservableProperty] private string? metaJson;
        [ObservableProperty] private WebCrawlingTask? meta;

        async partial void OnMetaJsonChanged(string? value)
        {
            this.Meta = value is null ? null : await JsonHelper.DeserializeAsync<WebCrawlingTask>(value);
        }
    }

    public class WebCrawlingTask
    {
    }
}