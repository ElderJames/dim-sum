// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AntDesign.Docs.Generator.Utils;
using ColorCode;
using Markdig;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace AntDesign.Docs.Generator
{
    [Generator]
    public class DemoGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var files = context.AdditionalTextsProvider
                .Where(static (text) => text.Path.Replace('\\', '/').Contains("/Demos/"))
                .Select(static (text, token) => (file: text, content: text.GetText(token)!.ToString()))
                .Collect();

            var compilationAndFiles = context.CompilationProvider.Combine(files);

            context.RegisterSourceOutput(compilationAndFiles, static (ctx, pair) =>
            {
                var compilation = pair.Left;
                var files = pair.Right;
                var docDir = files.Where(x => x.file.Path.Replace('\\', '/').Contains("/doc/"));
                var demoDir = files.Where(x => x.file.Path.Replace('\\', '/').Contains("/demo/"));

                var components = docDir.GroupBy(static x => GetComponentName(x.file.Path))
                .Where(x => x.Count() == 2)
                .ToDictionary(x => x.Key, x =>
                {
                    return x.ToDictionary(o => GetLocale(o.file.Path), o =>
                    {
                        var language = GetLocale(o.file.Path);
                        (Dictionary<string, string> Meta, string desc, string apiDoc) docData = DocParser.ParseDemoDoc(o.content);

                        return new DemoComponent
                        {
                            Category = docData.Meta["category"],
                            Title = docData.Meta["title"],
                            SubTitle = docData.Meta.TryGetValue("subtitle", out string subtitle) ? subtitle : null,
                            Type = docData.Meta["type"],
                            Desc = docData.desc,
                            ApiDoc = docData.apiDoc.Replace("<h2>API</h2>", $"<h2 id=\"API\"><span>API</span><a href=\"{language}/components/{docData.Meta["title"].ToLower()}#API\" class=\"anchor\">#</a></h2>"),
                            Cols = docData.Meta.TryGetValue("cols", out var cols) ? int.Parse(cols) : (int?)null,
                            Cover = docData.Meta.TryGetValue("cover", out var cover) ? cover : null,
                        };
                    });
                });

                // Pair each demo markdown file with its Razor file instead of requiring
                // exactly two files to share a punctuation-insensitive name. Names such as
                // `Head.razor` and the legacy `Head_.razor` normalize alike; the exact stem
                // match must win so an unrelated file cannot suppress the documented demo.
                var demoFiles = demoDir.ToArray();
                foreach (var md in demoFiles.Where(file => file.file.Path.EndsWith(".md")))
                {
                    var componentName = GetComponentName(md.file.Path);
                    if (!components.TryGetValue(componentName, out var componentLanguages))
                    {
                        continue;
                    }

                    var markdownStem = Path.GetFileNameWithoutExtension(md.file.Path);
                    var normalizedName = GetDemoName(md.file.Path);
                    var candidates = demoFiles.Where(file => file.file.Path.EndsWith(".razor")
                            && GetComponentName(file.file.Path) == componentName
                            && GetDemoName(file.file.Path) == normalizedName)
                        .ToArray();
                    var exactMatches = candidates.Where(file => string.Equals(
                            Path.GetFileNameWithoutExtension(file.file.Path), markdownStem,
                            StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    var razor = exactMatches.Length == 1
                        ? exactMatches[0]
                        : exactMatches.Length == 0 && candidates.Length == 1 ? candidates[0] : default;

                    if (razor.file == null)
                    {
                        continue;
                    }

                    (DescriptionYaml Meta, string Style, Dictionary<string, string> Descriptions) descriptionContent = DocParser.ParseDescription(md.content);

                    foreach (var title in descriptionContent.Meta.Title)
                    {
                        if (!componentLanguages.TryGetValue(title.Key, out var component) || !descriptionContent.Descriptions.TryGetValue(title.Key, out var description))
                        {
                            continue;
                        }
                        List<DemoItem> list = component.DemoList ??= new List<DemoItem>();

                        list.Add(new DemoItem()
                        {
                            Title = title.Value,
                            Order = descriptionContent.Meta.Order,
                            Iframe = descriptionContent.Meta.Iframe,
                            Link = descriptionContent.Meta.Link,
                            Code = razor.content,
                            Description = description,
                            Name = Path.GetFileNameWithoutExtension(md.file.Path),
                            Style = descriptionContent.Style,
                            Debug = descriptionContent.Meta.Debug,
                            Docs = descriptionContent.Meta.Docs,
                            AdditionalFiles = descriptionContent.Meta.AdditionalFiles?.Select(path =>
                            {
                                var normalizedPath = NormalizePath(path).TrimStart('/');
                                var demoDirectory = NormalizePath(md.file.Path).Substring(0, NormalizePath(md.file.Path).LastIndexOf('/') + 1);
                                var additionalFile = files.FirstOrDefault(file => NormalizePath(file.file.Path).EndsWith(demoDirectory + normalizedPath, StringComparison.Ordinal));
                                return (path, content: additionalFile.file == null ? null : additionalFile.content);
                            }).Where(file => file.content != null).ToDictionary(file => file.path, file => file.content),
                            TypeName = compilation.AssemblyName + NormalizePath(razor.file.Path).Substring(NormalizePath(razor.file.Path).IndexOf("/Demos/", StringComparison.Ordinal)).Replace("/", ".").Replace(".razor", ""),
                            SourcePath = NormalizePath(razor.file.Path).Substring(NormalizePath(razor.file.Path).IndexOf("/Demos/", StringComparison.Ordinal) + 1),
                        });
                    }
                }
                foreach (var componentPair in components)
                {
                    foreach (var component in componentPair.Value)
                    {
                        var className = $"{component.Key}{componentPair.Key}".Replace("/", "_").Replace("-", "_");
                        var source = SourceCodeGenerator.GenerateSourceCode(component.Value, compilation.AssemblyName, className);
                        ctx.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));

                        var pageSource = GetComponentCode(compilation.AssemblyName, $"/{component.Key}/{componentPair.Key}", className);
                        ctx.AddSource($"{className}.page.g.cs", SourceText.From(pageSource, Encoding.UTF8));
                    }
                }

                var indexSource = new StringBuilder();
                indexSource.AppendLine($"namespace {compilation.AssemblyName};")
                    .AppendLine("public static class GeneratedDemoIndex")
                    .AppendLine("{")
                    .AppendLine("    public static global::AntDesign.Docs.DemoComponent[] GetComponents(string language) => language switch")
                    .AppendLine("    {");
                foreach (var language in new[] { "en-US", "zh-CN" })
                {
                    var dataClasses = components
                        .Where(pair => pair.Value.ContainsKey(language))
                        .Select(pair => $"{language}{pair.Key}".Replace("/", "_").Replace("-", "_"))
                        .OrderBy(name => name, StringComparer.Ordinal);
                    indexSource.Append("        \"").Append(language).Append("\" => new global::AntDesign.Docs.DemoComponent[] { ")
                        .Append(string.Join(", ", dataClasses.Select(name => $"{name}.Data"))).AppendLine(" },");
                }
                indexSource.AppendLine("        _ => global::System.Array.Empty<global::AntDesign.Docs.DemoComponent>(),")
                    .AppendLine("    };")
                    .AppendLine()
                    .AppendLine("    public static readonly global::System.Collections.Generic.IReadOnlyDictionary<string, global::AntDesign.Docs.Components.SemanticPreviewRegistration[]> SemanticPreviews = CreateSemanticPreviews();")
                    .AppendLine()
                    .AppendLine("    private static global::System.Collections.Generic.IReadOnlyDictionary<string, global::AntDesign.Docs.Components.SemanticPreviewRegistration[]> CreateSemanticPreviews()")
                    .AppendLine("    {")
                    .AppendLine("        var previews = new global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<global::AntDesign.Docs.Components.SemanticPreviewRegistration>>(global::System.StringComparer.OrdinalIgnoreCase);");

                foreach (var componentPair in components)
                {
                    var component = componentPair.Value.TryGetValue("en-US", out var english)
                        ? english
                        : componentPair.Value.Values.FirstOrDefault();
                    var previewDemo = component?.DemoList?
                        .OrderBy(static demo => demo.Order)
                        .FirstOrDefault(static demo => string.Equals(demo.Name, "_semantic", System.StringComparison.OrdinalIgnoreCase))
                        ?? component?.DemoList?.OrderBy(static demo => demo.Order).FirstOrDefault();
                    if (previewDemo == null || string.IsNullOrWhiteSpace(previewDemo.TypeName))
                    {
                        continue;
                    }

                    var key = ToKebabCase(component.Title);
                    var wrap = previewDemo.Code?.Contains("<SemanticDomPreview", StringComparison.Ordinal) != true;
                    indexSource.Append("        previews[\"").Append(key).Append("\"] = new global::System.Collections.Generic.List<global::AntDesign.Docs.Components.SemanticPreviewRegistration> { new(\"")
                        .Append(key).Append("\", typeof(global::").Append(previewDemo.TypeName).Append("), ")
                        .Append(wrap ? "true" : "false").AppendLine(") }; ");
                }

                indexSource.AppendLine()
                    .AppendLine("        // Explicit semantic preview components replace generic demo previews where they provide a more representative DOM.");
                AddPreviewOverride(indexSource, "app", "AntDesign.Docs.Components.AppSemanticPreview");
                AddPreviewOverride(indexSource, "message", "AntDesign.Docs.Components.MessageSemanticPreview");
                AddPreviewOverride(indexSource, "notification", "AntDesign.Docs.Components.NotificationSemanticPreview");
                AddPreviewOverride(indexSource, "cascader", "AntDesign.Docs.Components.CascaderSemanticPreview");
                AddPreviewOverride(indexSource, "image", "AntDesign.Docs.Components.ImageSemanticPreview");
                AddPreviewOverride(indexSource, "menu", "AntDesign.Docs.Components.MenuSemanticPreview");
                AddPreviewOverride(indexSource, "collapse", "AntDesign.Docs.Components.CollapseSemanticPreview");
                AddPreviewOverride(indexSource, "tag", "AntDesign.Docs.Components.TagSemanticPreview");
                AddPreviewOverride(indexSource, "splitter", "AntDesign.Docs.Components.SplitterSemanticPreview");
                AddPreviewOverride(indexSource, "tooltip", "AntDesign.Docs.Components.TooltipSemanticPreview");
                AddPreviewOverride(indexSource, "dropdown", "AntDesign.Docs.Components.DropdownSemanticPreview");
                AddPreviewOverride(indexSource, "qr-code", "AntDesign.Docs.Components.QRCodeSemanticPreview");
                AddPreviewSupplement(indexSource, "float-button", "float-button-group", "AntDesign.Docs.Components.FloatButtonGroupSemanticPreview");
                AddPreviewSupplement(indexSource, "card", "card-meta", "AntDesign.Docs.Components.CardMetaSemanticPreview");

                indexSource.AppendLine("        var registrations = global::System.Linq.Enumerable.ToDictionary(previews, pair => pair.Key, pair => pair.Value.ToArray(), global::System.StringComparer.OrdinalIgnoreCase);")
                    .AppendLine("        return new global::System.Collections.ObjectModel.ReadOnlyDictionary<string, global::AntDesign.Docs.Components.SemanticPreviewRegistration[]>(registrations);")
                    .AppendLine("    }")
                    .AppendLine("}");
                ctx.AddSource("GeneratedDemoIndex.g.cs", SourceText.From(indexSource.ToString(), Encoding.UTF8));
            });
        }

        private static string GetDemoName(string filePath)
        {
            return new string(Path.GetFileNameWithoutExtension(filePath)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static void AddPreviewOverride(StringBuilder source, string key, string typeName)
        {
            source.Append("        previews[\"").Append(key).Append("\"] = new global::System.Collections.Generic.List<global::AntDesign.Docs.Components.SemanticPreviewRegistration> { new(\"")
                .Append(key).Append("\", typeof(global::").Append(typeName).AppendLine("), true) };");
        }

        private static void AddPreviewSupplement(StringBuilder source, string key, string component, string typeName)
        {
            var localName = "previewsFor" + string.Concat(key.Split('-').Select(static part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
            source.Append("        if (!previews.TryGetValue(\"").Append(key).Append("\", out var ").Append(localName).Append(")) previews[\"")
                .Append(key).Append("\"] = ").Append(localName).Append(" = new global::System.Collections.Generic.List<global::AntDesign.Docs.Components.SemanticPreviewRegistration>();")
                .Append("\n        ").Append(localName).Append(".Add(new global::AntDesign.Docs.Components.SemanticPreviewRegistration(\"")
                .Append(component).Append("\", typeof(global::").Append(typeName).AppendLine("), true));");
        }

        private static string ToKebabCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var result = new StringBuilder(value.Length + 8);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsUpper(character) && index > 0 &&
                    (char.IsLower(value[index - 1]) || char.IsDigit(value[index - 1]) ||
                     (char.IsUpper(value[index - 1]) && index + 1 < value.Length && char.IsLower(value[index + 1]))))
                {
                    result.Append('-');
                }

                result.Append(char.ToLowerInvariant(character));
            }

            return result.ToString();
        }
        private static string GetComponentName(string filePath)
        {
            var parts = NormalizePath(filePath).Split('/');
            var demoIndex = Array.IndexOf(parts, "Demos");
            return $"{parts[demoIndex + 1]}/{parts[demoIndex + 2]}";
        }

        private static string GetLocale(string filePath)
        {
            return Path.GetFileName(filePath).Replace("index.", "").Replace(".md", "");
        }

        private static string NormalizePath(string path) => path.Replace('\\', '/');

        private static string GetComponentCode(string nameSpace, string path, string demoComponentName)
        {
            var template = """
                using AntDesign.Docs.Components;
                using Microsoft.AspNetCore.Components;
                using Microsoft.AspNetCore.Components.Rendering;

                namespace {{namespace}};

                [Route("{{path}}")]
                public class {{className}}Component : ComponentBase
                {
                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.OpenComponent<ComponentDoc>(0);

                        builder.AddAttribute(1, nameof(ComponentDoc.DemoComponent), {{demoComponentName}}.Data);

                        builder.CloseComponent();
                    }
                }
                """;

            return template.Replace("{{namespace}}", nameSpace)
                .Replace("{{path}}", path)
                .Replace("{{className}}", demoComponentName)
                .Replace("{{demoComponentName}}", demoComponentName);
        }
    
        private static string ParseCode(string code)
        {
            var formatter = new HtmlFormatter();

            var csharpIndex = code.IndexOf("@code");

            if (csharpIndex > 0)
            {
                var htmlPart = code.Substring(0, csharpIndex);
                var csharpPart = code.Substring(csharpIndex);

                return $@"""
                {formatter.GetHtmlString(htmlPart, Languages.Html)}

                {formatter.GetHtmlString(csharpPart, Languages.CSharp)}
                """;
            }
       
            return formatter.GetHtmlString(code, Languages.Html);
        }
    }
}
