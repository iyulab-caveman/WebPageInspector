using WebPageInspector.Models;

namespace WebPageInspector.Services
{
    public class WebCrawlingTaskService
    {
        public Task<IEnumerable<WebCrawlingTask>> GetTemplatesAsync()
        {
            return Task.Run(() =>
            {
                var basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples");
                var files = System.IO.Directory.GetFiles(basePath, "*.json");

                var tasks = files.Select(file => WebCrawlingTask.FromJson(System.IO.File.ReadAllText(file)));
                return tasks;
            });
        }
    }
}
