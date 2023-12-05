using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HtmlAgilityPack;
using Iyu.Windows;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Text.RegularExpressions;
using WebPageInspector.Behaviors;
using WebPageInspector.Contexts;

namespace WebPageInspector.Controls
{
    [INotifyPropertyChanged]
    public sealed partial class WebPageContextTool : UserControl
    {
        [ObservableProperty] private string? expression;
        [ObservableProperty] private IEnumerable<string>? matches;

        public WebPageContextTool()
        {
            this.InitializeComponent();
        }

        public WebPageContext Context
        {
            get { return (WebPageContext)GetValue(ContextProperty); }
            set { SetValue(ContextProperty, value); }
        }

        public static readonly DependencyProperty ContextProperty =
            DependencyProperty.Register("Context", typeof(WebPageContext), typeof(WebPageContextTool), new PropertyMetadata(default));

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
                    if (Context.WebView is WebView2 wv && wv.GetHtml() is string html)
                    {
                        try
                        {
                            var r = await Task.Run(() =>
                            {
                                var text = HtmlHelper.GetText(html);
                                if (text == null) return Array.Empty<string>();

                                var regex = new Regex(Expression);
                                var matches = regex.Matches(text);
                                return matches.Select(p => p.Value);
                            });
                            this.Matches = r;
                        }
                        catch (Exception)
                        {
                        }
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
    }

    public class HtmlNodeTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item)
        {
            var xaml = $@"
    <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
        <TreeViewItem ItemsSource=""{{Binding Children}}"" 
                      Content=""{{Binding}}""  />
    </DataTemplate>";

            var template = XamlReader.Load(xaml) as DataTemplate;
            return template ?? base.SelectTemplateCore(item);
        }
    }
}