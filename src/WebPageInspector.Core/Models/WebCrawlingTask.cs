using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;

namespace WebPageInspector.Models
{
    public class WebCrawlingTask
    {
        public string Name { get; init; } = null!;
        public string? Description { get; set; }
        public IList<IWebCrawlingAction> Steps { get; init; } = null!;
        public WebCrawlingOutput Output { get; init; } = null!;

        public string BuildJson()
        {
            return JsonHelper.Serialize(this);
        }

        public static WebCrawlingTask FromJson(string json)
        {
            return JsonHelper.Deserialize<WebCrawlingTask>(json) ?? throw new Exception("cannot parse WebCrawlingTask");
        }
    }

    [JsonConverter(typeof(WebCrawlingActionConverter))]
    public interface IWebCrawlingAction
    {
        Dictionary<string, string> Variables { get; }
    }

    public abstract class WebCrawlingActionBase : IWebCrawlingAction
    {
        public abstract string Type { get; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public Dictionary<string, string> Variables { get; set; } = [];
    }

    public class WebCrawlingSearchAction : WebCrawlingActionBase
    {
        public override string Type => "search";
        public string Url { get; init; } = null!;
        public IList<NextPage>? NextPages { get; set; }

        public class NextPage
        {
            public string StartWith { get; init; } = null!;
        }
    }

    public class WebCrawlingNavigateAction : WebCrawlingActionBase
    {
        public override string Type => "navigate";
        public IList<Rule> Rules { get; init; } = null!;

        public class Rule
        {
            public string PageIdentity { get; init; } = null!;
            public string? StartWith { get; set; }
            public string? Match { get; set; }
        }
    }

    public class WebCrawlingExtractAction : WebCrawlingActionBase
    {
        public override string Type => "extract";
        public IList<Rule> Rules { get; init; } = null!;

        public class Rule
        {
            public string PageIdentity { get; init; } = null!;
            public IList<Field> Fields { get; init; } = null!;
        }

        public class Field
        {
            public string Name { get; init; } = null!;
            public string? Variable { get; set; }
            public string? Selector { get; set; }
            public string? XPath { get; set; }
        }
    }

    public class WebCrawlingOutput
    {
        public string Format { get; init; } = null!;

        [JsonConverter(typeof(OutputStorageConverter))]
        public IOutputStorage Storage { get; init; } = null!;
    }

    public interface IOutputStorage
    {
    }

    public class OutputFileSystem : IOutputStorage
    {
        public string Type => "file-system";
        public string Path { get; init; } = null!;
    }

    public class WebCrawlingActionConverter : JsonConverter<IWebCrawlingAction>
    {
        public override IWebCrawlingAction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
            if (dic != null && dic.TryGetValueWithIgnoreCase("Type", out JsonElement value))
            {
                var type = value.GetString();
                var json = JsonSerializer.Serialize(dic, options);
                return type switch
                {
                    "search" => JsonSerializer.Deserialize<WebCrawlingSearchAction>(json, options)!,
                    "navigate" => JsonSerializer.Deserialize<WebCrawlingNavigateAction>(json, options)!,
                    "extract" => JsonSerializer.Deserialize<WebCrawlingExtractAction>(json, options)!,
                    _ => throw new JsonException($"Unknown type {type}"),
                };
            }

            throw new JsonException("Type field is missing");
        }

        public override void Write(Utf8JsonWriter writer, IWebCrawlingAction value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    public class OutputStorageConverter : JsonConverter<IOutputStorage>
    {
        public override IOutputStorage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
            if (dic != null && dic.TryGetValueWithIgnoreCase("Type", out JsonElement value))
            {
                var type = value.GetString();
                var json = JsonSerializer.Serialize(dic, options);
                return type switch
                {
                    "file-system" => JsonSerializer.Deserialize<OutputFileSystem>(json, options)!,
                    _ => throw new JsonException($"Unknown type {type}"),
                };
            }

            throw new JsonException("Type field is missing");
        }

        public override void Write(Utf8JsonWriter writer, IOutputStorage value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

}
