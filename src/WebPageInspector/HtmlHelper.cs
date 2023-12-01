using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace WebPageInspector
{
    public static partial class HtmlHelper
    {
        public static string? GetText(string html)
        {
            // HTML 문서 로드
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // 스크립트와 스타일 태그를 더 효율적으로 제거
            htmlDoc.DocumentNode.Descendants()
                    .Where(n => n.Name == "script" || n.Name == "style")
                    .ToList()
                    .ForEach(n => n.Remove());

            // 순수 텍스트 추출
            var text = htmlDoc.DocumentNode.InnerText;

            // HTML 엔티티를 일반 텍스트로 변환 및 정규식 적용
            // 여기에서는 개행과 공백을 동시에 처리합니다.
            text = System.Net.WebUtility.HtmlDecode(text);
            text = Regex_TextRemoveSpace().Replace(text, m => m.Value.StartsWith("\n") ? "\n" : " ").Trim();

            return text;
        }

        [GeneratedRegex(@"(\n{2,})|(\s{2,})")]
        private static partial Regex Regex_TextRemoveSpace();
    }
}
