using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WebPageInspector.Models;
using WebPageInspector.Services;

namespace WebPageInspector.Prod
{
    public class Startup : StartupBase
    {
#if DEBUG

        protected override void OnActivated()
        {
            base.OnActivated();

            //var test = new UITest();
            //test.Run();
        }
#endif
    }

    #region DEBUG

    public class UITest
    {
        internal async void Run()
        {
            var path = "Samples/google-scholar.json";
            var filePath = Path.Combine(AppDomain   .CurrentDomain.BaseDirectory, path);
            var json = File.ReadAllText(filePath);

            var task = WebCrawlingTask.FromJson(json);

            var crawler = new Crawler();
            await crawler.RunAsync(task);

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    #endregion
}