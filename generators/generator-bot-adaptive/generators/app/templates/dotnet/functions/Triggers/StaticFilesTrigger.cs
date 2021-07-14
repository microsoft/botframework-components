using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace <%= botName %>.Triggers
{
    public class StaticFilesTrigger
    {
        private const string StaticFilesDirectory = "wwwroot";

        private readonly FileExtensionContentTypeProvider _provider;

        public StaticFilesTrigger()
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".lu"] = "application/vnd.microsoft.lu";
            provider.Mappings[".qna"] = "application/vnd.microsoft.qna";

            _provider = provider;
        }

        [FunctionName("StaticFiles")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "{*path}")] HttpRequest request,
            string path,
            ExecutionContext context)
        {
            if (!TryGetFilePath(context, path, out string filePath) ||
                !_provider.TryGetContentType(filePath, out string contentType))
            {
                return new NotFoundResult();
            }

            return new FileStreamResult(new FileStream(filePath, FileMode.Open), contentType);
        }

        private bool TryGetFilePath(ExecutionContext context, string relativePath, out string result)
        {
            result = null;

            if (string.IsNullOrEmpty(relativePath))
            {
                return false;
            }

            string filePath = Path.GetFullPath(
                Path.Join(
                    context.FunctionAppDirectory,
                    StaticFilesDirectory,
                    relativePath));

            var file = new FileInfo(filePath);
            if (!file.Exists)
            {
                return false;
            }

            result = file.FullName;
            return true;
        }
    }
}
