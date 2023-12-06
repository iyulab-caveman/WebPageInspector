using System.Text.Json;
using WebPageInspector.Models;
using WebPageInspector.Services;

namespace WebPageInspector.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void ParsingTest()
        {
            var path = "google-scholar.json";
            var json = File.ReadAllText(path);

            var task = WebCrawlingTask.FromJson(json);
            Assert.True(true);
        }
    }
}