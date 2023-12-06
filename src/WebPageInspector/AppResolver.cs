using Iyu;
using WebPageInspector.Services;

namespace WebPageInspector
{
    public static class AppResolver
    {
        public static WebCrawlingTaskService TaskService => IoC.Resolve<WebCrawlingTaskService>();
    }
}
