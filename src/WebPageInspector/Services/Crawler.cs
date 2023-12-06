using Iyu;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System.Text.RegularExpressions;
using WebPageInspector.Behaviors;
using WebPageInspector.Contexts;
using WebPageInspector.Models;
using Windows.ApplicationModel.Contacts;

namespace WebPageInspector.Services
{
    public class ContextWebView
    {
        private readonly WebView2 wv;
        private TaskCompletionSource<bool>? navigationCompletedTaskSource;

        public ContextWebView() : this(new WebView2())
        {
        }

        public ContextWebView(WebView2 wv)
        {
            this.wv = wv;
            wv.NavigationCompleted += ContextWebView_NavigationCompleted;
        }

        private void ContextWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (navigationCompletedTaskSource == null) return;

            if (args.IsSuccess == false)
                navigationCompletedTaskSource.SetException(new Exception(args.WebErrorStatus.ToString()));
            
            else if (navigationCompletedTaskSource.Task.IsCompleted == false)
                navigationCompletedTaskSource.SetResult(true);
        }

        internal async Task<WebPageContext> NavigateAsync(string url)
        {
            navigationCompletedTaskSource = new TaskCompletionSource<bool>();
            wv.Source = new Uri(url);
            await navigationCompletedTaskSource.Task;

            var html = await wv.GetHtmlAsync();
            var context = new WebPageContext(wv, html);
            return context;
        }

        internal static Task<WebPageContext> GetNewContextAsync(string url)
        {
            var wv = new ContextWebView();
            return wv.NavigateAsync(url);
        }
    }

    public class Crawler
    {
        private readonly ContextWebView wv;

        public Crawler()
        {
            this.wv = new ContextWebView();
        }

        public Crawler(WebView2 wv)
        {
            this.wv = new ContextWebView(wv);
        }

        public async Task<object> RunAsync(WebCrawlingTask task)
        {
            LogService.Info($"Crawler Start Task: {task.Name}");

            var c = new CrawlingContext();
            foreach (var step in task.Steps)
            {
                await PipelineAsync(c, step);
            }

            LogService.Info($"Crawler Done Task: {task.Name}");

            return c.GetResult();
        }

        private async Task PipelineAsync(CrawlingContext c, IWebCrawlingAction step)
        {
            c.ApplyVariables(step.Variables);

            if (step is WebCrawlingSearchAction searchAction)
                await SearchAsync(c, searchAction);

            else if (step is WebCrawlingNavigateAction navigateAction)
                await NavigateAsync(c, navigateAction);

            else if (step is WebCrawlingExtractAction extractAction)
                await ExtractAsync(c, extractAction);

            else
                throw new NotSupportedException(step.GetType().FullName);
        }

        private async Task SearchAsync(CrawlingContext c, WebCrawlingSearchAction searchAction)
        {
            var url = c.BuildUrl(searchAction.Url);
            var context = await wv.NavigateAsync(url);
            c.Context = context;
        }

        private async Task NavigateAsync(CrawlingContext c, WebCrawlingNavigateAction navigateAction)
        {
            if (c.Context == null) return;
            var links = await c.Context.GetLinksAsync();

            c.InitNavigateLinks();
            foreach(var link in links)
            {
                foreach(var rule in navigateAction.Rules)
                {
                    if (rule.StartWith is string startWith)
                    {
                        if (link.StartsWith(startWith))
                        {
                            c.AddNavigateLink(new NavigateLinkItem()
                            { 
                                PageIdentity = rule.PageIdentity,
                                Url = link
                            });
                        }
                    }
                    else if (rule.Match is string regex)
                    {
                        var r = new Regex(regex);
                        if (r.IsMatch(link))
                        {
                            c.AddNavigateLink(new NavigateLinkItem()
                            {
                                PageIdentity = rule.PageIdentity,
                                Url = link
                            });
                        }
                    }
                }
            }
        }

        private async Task ExtractAsync(CrawlingContext c, WebCrawlingExtractAction extractAction)
        {
            await foreach (var (navigateLink, context) in c.NextAsync(wv))
            {
                var rule = extractAction.Rules.FirstOrDefault(x => x.PageIdentity == navigateLink.PageIdentity);
                if (rule == null) continue;

                var data = new Dictionary<string, string>();
                foreach(var field in rule.Fields)
                {
                    var value = await GetFieldValueAsync(c, context, field);
                    data[field.Name] = value;
                }
                c.AddRow(data);
            }
        }

        private async Task<string> GetFieldValueAsync(CrawlingContext crawlingContext, WebPageContext webPagecontext, WebCrawlingExtractAction.Field field)
        {
            if (field.Variable is string variable)
            {
                return crawlingContext.GetVariable(variable, webPagecontext);
            }
            else if (field.Selector is string selector)
            {
                return await webPagecontext.GetElementTextBySelectorAsync(selector);
            }
            else if (field.XPath is string xpath)
            {
                return await webPagecontext.GetElementTextByXPathAsync(xpath);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public partial class CrawlingContext
    {
        private readonly Dictionary<string, string> variables = [];
        public WebPageContext? Context { get; internal set; }
        private List<NavigateLinkItem> navigateLinks = [];
        private int navigateLinkIndex = 0;

        private readonly List<Dictionary<string, string>> data = [];

        public void ApplyVariables(Dictionary<string, string> variables)
        {
            foreach (var (key, value) in variables)
            {
                this.variables[key] = value;
            }
        }

        internal string BuildUrl(string url)
        {
            var r = VariableRegex().Replace(url, match =>
            {
                var key = match.Groups[1].Value;
                return variables.TryGetValue(key, out string? value) ? value : match.Value;
            });

            return r;
        }

        [GeneratedRegex(@"\$\{([^}]+)\}")]
        private static partial Regex VariableRegex();

        internal void InitNavigateLinks()
        {
            navigateLinks.Clear();
            navigateLinkIndex = 0;
        }

        internal void AddNavigateLink(NavigateLinkItem navigateLinkItem)
        {
            navigateLinks.Add(navigateLinkItem);
        }

        internal async IAsyncEnumerable<(NavigateLinkItem, WebPageContext)> NextAsync(ContextWebView wv)
        {   
            while (navigateLinks.Count > navigateLinkIndex)
            {
                var navigateLink = navigateLinks[navigateLinkIndex];
                navigateLinkIndex++;

                var url = BuildUrl(navigateLink.Url);
                var context = await wv.NavigateAsync(url);
                yield return (navigateLink, context);
            }
        }

        internal void AddRow(Dictionary<string, string> row)
        {
            this.data.Add(row);
        }

        internal string GetVariable(string variable, WebPageContext webPagecontext)
        {
            if (variable.Equals("@Url", StringComparison.OrdinalIgnoreCase))
                return webPagecontext.Url;

            else if (variables.TryGetValue(variable, out string? value))
                return value;
            else
                return string.Empty;
        }

        internal object GetResult()
        {
            return this.data;
        }
    }

    public class NavigateLinkItem
    {
        public required string PageIdentity { get; set; }
        public required string Url { get; set; }
    }
}
