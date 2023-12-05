using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;
using HtmlAgilityPack;
using Iyu.Windows.Mvvm;
using Iyu.Windows;
using Iyu;

namespace WebPageInspector.Contexts
{
    public partial class WebPageContext : ObservableObject
    {
        [GeneratedRegex(@"\bhttps?://[^\s<>""]+(\?[^\s<>""]*)?")]
        private static partial Regex Regex_LINK();

        [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")]
        private static partial Regex Regex_EMAIL();

        private readonly WeakReference<WebView2> wvRef;
        public WebView2? WebView => wvRef.TryGetTarget(out var wv) ? wv : null;

        [ObservableProperty] private string? html;
        [ObservableProperty] private HtmlDocument? htmlDoc;

        private string url;
        private string? text;
        private string? title;
        private IEnumerable<HtmlNodeContext>? nodes;

        private IEnumerable<string>? metadatas;
        private IEnumerable<string>? links;
        private IEnumerable<string>? images;
        private IEnumerable<string>? emails;

        public IEnumerable<string>? Metadatas { get => this.GetPropertyValue(ref metadatas, InitMetadatasAsync); set => this.SetPropertyValue(ref metadatas, value); }
        public IEnumerable<string>? Links { get => this.GetPropertyValue(ref links, InitLinksAsync); set => this.SetPropertyValue(ref links, value); }
        public IEnumerable<string>? Images { get => this.GetPropertyValue(ref images, InitImagesAsync); set => this.SetPropertyValue(ref images, value); }
        public IEnumerable<string>? Emails { get => this.GetPropertyValue(ref emails, InitEmailsAsync); set => this.SetPropertyValue(ref emails, value); }

        public string? Text => text ??= GetText();
        public string? Title => title ??= GetTitle();
        public IEnumerable<HtmlNodeContext> Nodes => nodes ??= HtmlNodeContext.Build(this.HtmlDoc!);
        [ObservableProperty] private HtmlNodeContext? selectedNode;

        public WebPageContext(WebView2 wv, string? html)
        {
            this.wvRef = new WeakReference<WebView2>(wv);
            this.html = html;

            this.url = wv.Source.OriginalString;

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            this.htmlDoc = htmlDoc;

            InjectHighlightFunctions(wv);
        }

        private async void InjectHighlightFunctions(WebView2 webView)
        {
            string script = @"
function removeHighlightOverlay() {
    var existingOverlay = document.getElementById('highlightOverlay');
    if (existingOverlay) {
        existingOverlay.remove();
    }
}

function highlightNode(xpath) {
    removeHighlightOverlay();

    var xpathResult = document.evaluate(xpath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    var targetNode = xpathResult.singleNodeValue;

    if (targetNode) {
        var rect = targetNode.getBoundingClientRect();

        var overlay = document.createElement('div');
        overlay.id = 'highlightOverlay';
        overlay.style.position = 'absolute';
        overlay.style.left = rect.left + 'px';
        overlay.style.top = rect.top + 'px';
        overlay.style.width = rect.width + 'px';
        overlay.style.height = rect.height + 'px';
        overlay.style.backgroundColor = 'rgba(255, 255, 0, 0.5)'; // 반투명 노란색
        overlay.style.pointerEvents = 'none'; // 클릭 이벤트 방지
        overlay.style.zIndex = 1000; // 높은 z-index 값

        document.body.appendChild(overlay);
    }
}";

            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private string? GetText()
        {
            var html = Html;
            if (string.IsNullOrWhiteSpace(html)) return null;

            var text = HtmlHelper.GetText(html);
            return text;
        }

        private string? GetTitle()
        {
            if (HtmlDoc == null) return null;

            return HtmlDoc.DocumentNode.SelectSingleNode("//title")?.InnerText;
        }

        private async Task<IEnumerable<string>> InitMetadatasAsync()
        {
            if (HtmlDoc == null) return Enumerable.Empty<string>();

            var metadatas = new List<string>();

            // <meta> 태그에서 메타데이터 추출
            var metaNodes = HtmlDoc.DocumentNode.SelectNodes("//meta");
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

            return await Task.FromResult(metadatas);
        }

        private async Task<IEnumerable<string>> InitLinksAsync()
        {
            if (HtmlDoc == null) return Enumerable.Empty<string>();

            var links = new List<string>();

            // <a> 태그 및 <link> 태그에서 링크 추출
            var linkNodes = HtmlDoc.DocumentNode.SelectNodes("//a[@href] | //link[@href and @rel='stylesheet']");
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

            // 이미지 형식의 링크는 제외
            var exceptNames = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico" };

            // 텍스트 노드에서 "http://" 또는 "https://"로 시작하는 모든 URL 찾기
            var textNodes = HtmlDoc.DocumentNode.SelectNodes("//text()[contains(., 'http://') or contains(., 'https://')]");
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
                            // 이미지 형식의 링크는 제외
                            if (exceptNames.Any(p => match.Value.EndsWith(p))) continue;

                            var v = match.Value.LeftOr(")}");
                            links.Add(v);
                        }
                    }
                }
            }

            return await Task.FromResult(links.Distinct().ToList());
        }

        private async Task<IEnumerable<string>> InitImagesAsync()
        {
            var result = await UIHelper.BeginInvoke(async () =>
            {
                if (WebView == null) return null;

                try
                {
                    // JavaScript 코드를 작성하여 모든 이미지의 src 속성을 가져옵니다.
                    var script = @"Array.from(document.images).map(img => img.src);";
                    var result = await WebView.ExecuteScriptAsync(script);

                    return result;
                }
                catch (Exception ex)
                {
                    // 오류 처리
                    Debug.WriteLine("Error fetching image sources: " + ex.Message);
                    return null;
                }
            });

            if (string.IsNullOrWhiteSpace(result)) return Enumerable.Empty<string>();

            // 결과 처리 (JSON 형식으로 반환되므로 처리 필요)
            var jsonResult = System.Text.Json.JsonSerializer.Deserialize<string[]>(result);
            return jsonResult?.Where(p => p.Length > 0) ?? Enumerable.Empty<string>();
        }

        private async Task<IEnumerable<string>> InitEmailsAsync()
        {
            if (HtmlDoc == null) return Enumerable.Empty<string>();

            var emails = new List<string>();

            var regex = Regex_EMAIL();
            var text = HtmlDoc.DocumentNode.InnerText;

            var matches = regex.Matches(text);

            foreach (Match match in matches.Cast<Match>())
            {
                if (match.Success)
                {
                    emails.Add(match.Value.Trim());
                }
            }

            return await Task.FromResult(emails);
        }

        partial void OnSelectedNodeChanged(HtmlNodeContext? value)
        {
            // 기존 하이라이트 해제
            WebView?.ExecuteScriptAsync("removeHighlightOverlay();");

            if (value != null)
            {
                HtmlNode node = value.Node;
                string highlightScript = GenerateHighlightScriptByXPath(node);
                WebView?.ExecuteScriptAsync(highlightScript);
            }
        }

        private string GenerateHighlightScriptByXPath(HtmlNode node)
        {
            string xpath = node.XPath;
            string script = $@"highlightNode('{xpath}');";
            return script;
        }
    }

    public class HtmlNodeContext(HtmlNode node, string name)
    {
        public HtmlNode Node { get; } = node;
        public string Name { get; } = name;
        public string? Id => Node.GetAttrId();
        public string[] Classes => Node.GetAttrClasses();
        public string? Text => Node.GetInnerText();

        public IList<HtmlNodeContext> Children { get; } = new List<HtmlNodeContext>();

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Name);

            if (Id != null)
            {
                sb.Append($" #{Id}");
            }

            if (Classes.Length != 0)
            {
                sb.Append($" .{string.Join(".", Classes)}");
            }

            if (string.IsNullOrEmpty(Text) != true)
            {
                // 최대 10자리까지만 표시
                var text = Text;
                if (text.Length > 10)
                {
                    text = string.Concat(text.AsSpan(0, 10), "...");
                }
                sb.Append($" \"{text}\"");
            }

            return sb.ToString();
        }

        private static string[] GetSkipNames()
        {
            return ["#text", "#comment", "script", "meta", "title", "style", "link"];
        }

        public static IEnumerable<HtmlNodeContext> Build(HtmlDocument htmlDoc)
        {
            var skipNames = GetSkipNames();
            foreach(var node in htmlDoc.DocumentNode.ChildNodes)
            {
                if (skipNames.Contains(node.Name)) continue;
                yield return Build(node);
            }
        }

        private static HtmlNodeContext Build(HtmlNode node)
        {
            var skipNames = GetSkipNames();

            var context = new HtmlNodeContext(node, node.Name);

            foreach(var child in node.ChildNodes)
            {
                if (skipNames.Contains(child.Name)) continue;

                context.Children.Add(Build(child));
            }

            return context;
        }
    }

    public static class HtmlNodeExtensions
    {
        public static string? GetAttrId(this HtmlNode node)
        {
            // id 속성이 있는 경우 id 속성 값을 반환합니다.
            var id = node.GetAttributeValue("id", string.Empty);
            if (!string.IsNullOrEmpty(id)) return id;
            return null;
        }

        public static string[] GetAttrClasses(this HtmlNode node)
        {
            // class 속성이 있는 경우 class 속성 값을 반환합니다.
            var classes = node.GetAttributeValue("class", string.Empty);
            if (!string.IsNullOrEmpty(classes)) return classes.Split(' ');
            return [];
        }

        public static string? GetInnerText(this HtmlNode node)
        {
            return node.InnerText.Trim();
        }
    }
}