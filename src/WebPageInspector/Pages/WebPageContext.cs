using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;
using HtmlAgilityPack;
using Iyu.Windows.Mvvm;
using Iyu.Windows;

namespace WebPageInspector.Pages
{
    public partial class WebPageContext : ObservableObject
    {
        private readonly WebView2 wv;
        [ObservableProperty] private string? html;

        private string url;

        private string? text;
        public string? Text => text ??= GetText();

        private IEnumerable<string>? metadatas;
        public IEnumerable<string>? Metadatas { get => this.GetPropertyValue(ref metadatas, InitMetadatas); set => this.SetPropertyValue(ref metadatas, value); }

        private IEnumerable<string>? links;
        public IEnumerable<string>? Links { get => this.GetPropertyValue(ref links, InitLinks); set => this.SetPropertyValue(ref links, value); }

        private IEnumerable<string>? images;
        public IEnumerable<string>? Images { get => this.GetPropertyValue(ref images, InitImagesAsync); set => this.SetPropertyValue(ref images, value); }

        private IEnumerable<string>? emails;
        public IEnumerable<string>? Emails { get => this.GetPropertyValue(ref emails, InitEmails); set => this.SetPropertyValue(ref emails, value); }
        public WebPageContext(WebView2 wv, string? html)
        {
            this.wv = wv;
            this.html = html;

            this.url = wv.Source.OriginalString;
        }

        private string? GetText()
        {
            var html = Html;
            if (string.IsNullOrWhiteSpace(html)) return null;

            var text = HtmlHelper.GetText(html);
            return text;
        }

        public IEnumerable<string> InitMetadatas()
        {
            var html = Html;
            if (string.IsNullOrWhiteSpace(html))
                return Enumerable.Empty<string>();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var metadatas = new List<string>();

            // <meta> 태그에서 메타데이터 추출
            var metaNodes = htmlDoc.DocumentNode.SelectNodes("//meta");
            if (metaNodes != null)
            {
                foreach (var node in metaNodes)
                {
                    var name = node.GetAttributeValue("name", string.Empty);
                    var content = node.GetAttributeValue("content", string.Empty);
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
                    {
                        metadatas.Add($"{name}: {content}");
                    }
                }
            }

            return metadatas;
        }

        public IEnumerable<string> InitLinks()
        {
            var html = Html;
            if (string.IsNullOrWhiteSpace(html))
                return Enumerable.Empty<string>();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var links = new List<string>();

            // <a> 태그 및 <link> 태그에서 링크 추출
            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a[@href] | //link[@href and @rel='stylesheet']");
            if (linkNodes != null)
            {
                links.AddRange(linkNodes.Select(node =>
                {
                    var path = node.Name switch
                    {
                        "a" => node.GetAttributeValue("href", string.Empty),
                        "link" => node.GetAttributeValue("href", string.Empty),
                        _ => string.Empty
                    };
                    if (path.StartsWith("http"))
                        return path;
                    else
                        return $"{url}{path}";
                }));
            }   

            // 텍스트 노드에서 "http://" 또는 "https://"로 시작하는 모든 URL 찾기
            var textNodes = htmlDoc.DocumentNode.SelectNodes("//text()[contains(., 'http://') or contains(., 'https://')]");
            if (textNodes != null)
            {
                var urlRegex = Regex_LINK();
                foreach (var node in textNodes)
                {
                    var matches = urlRegex.Matches(node.InnerText);
                    foreach (Match match in matches.Cast<Match>())
                    {
                        if (match.Success)
                        {
                            links.Add(match.Value);
                        }
                    }
                }
            }

            return links.Distinct().ToList();
        }

        public Task<IEnumerable<string>> InitImagesAsync()
        {
            return UIHelper.BeginInvoke(async () =>
            {
                if (wv == null) return Enumerable.Empty<string>();

                try
                {
                    // JavaScript 코드를 작성하여 모든 이미지의 src 속성을 가져옵니다.
                    var script = @"Array.from(document.images).map(img => img.src);";
                    var result = await wv.ExecuteScriptAsync(script);

                    // 결과 처리 (JSON 형식으로 반환되므로 처리 필요)
                    var jsonResult = System.Text.Json.JsonSerializer.Deserialize<string[]>(result);
                    return jsonResult?.Where(p => p.Length > 0) ?? Enumerable.Empty<string>();
                }
                catch (Exception ex)
                {
                    // 오류 처리
                    Debug.WriteLine("Error fetching image sources: " + ex.Message);
                    return Enumerable.Empty<string>();
                }
            });
        }

        public IEnumerable<string> InitEmails()
        {
            var html = Html;
            if (string.IsNullOrWhiteSpace(html))
                return Enumerable.Empty<string>();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var emails = new List<string>();

            var regex = Regex_EMAIL();
            var text = htmlDoc.DocumentNode.InnerText;

            var matches = regex.Matches(text);

            foreach (Match match in matches.Cast<Match>())
            {
                if (match.Success)
                {
                    emails.Add(match.Value.Trim());
                }
            }

            return emails;
        }


        [GeneratedRegex(@"\bhttps?://[^\s<>""]+(\?[^\s<>""]*)?")]
        private static partial Regex Regex_LINK();

        [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")]
        private static partial Regex Regex_EMAIL();
    }
}