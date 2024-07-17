// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AntDesign.Docs.Generator.Utils;
using Markdig;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace AntDesign.Docs.Generator
{
    [Generator]
    public class DemoGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var files = context.AdditionalTextsProvider
                .Where(static (text) => text.Path.Contains("\\Demos\\"))
                .Select(static (text,token)=> (file:text, content: text.GetText(token)!.ToString()))
                .Collect();

          var compilationAndFiles=  context.CompilationProvider.Combine(files);

            context.RegisterSourceOutput(compilationAndFiles, static (ctx, pair) =>
            {
                var compilation = pair.Left;
                var files = pair.Right;
                var docDir = files.Where(x => x.file.Path.Contains("doc"));
                var demoDir = files.Where(x => x.file.Path.Contains("demo"));

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

                foreach (var x in demoDir.GroupBy(static x => GetDemoName(x.file.Path)).Where(x => x.Count() == 2))
                {
                    var razor = x.FirstOrDefault(x => x.file.Path.EndsWith(".razor"));
                    var md = x.FirstOrDefault(x => x.file.Path.EndsWith(".md"));

                    (DescriptionYaml Meta, string Style, Dictionary<string, string> Descriptions) descriptionContent = DocParser.ParseDescription(md.content);

                    foreach (var title in descriptionContent.Meta.Title)
                    {
                        var componentDic = components[GetComponentName(razor.file.Path)];
                        List<DemoItem> list = componentDic[title.Key].DemoList ??= new List<DemoItem>();

                        list.Add(new DemoItem()
                        {
                            Title = title.Value,
                            Order = descriptionContent.Meta.Order,
                            Iframe = descriptionContent.Meta.Iframe,
                            Link = descriptionContent.Meta.Link,
                            Code = razor.content,
                            Description = descriptionContent.Descriptions[title.Key],
                            Name = md.file.Path.Split('\\').Last().Replace(".md", ""),
                            Style = descriptionContent.Style,
                            Debug = descriptionContent.Meta.Debug,
                            Docs = descriptionContent.Meta.Docs,
                            TypeName = compilation.AssemblyName + razor.file.Path.Substring(razor.file.Path.IndexOf("\\Demos\\")).Replace("\\", ".").Replace(".razor", ""),
                        });
                    }
                }
                foreach (var componentPair in components)
                {
                    foreach(var component in componentPair.Value)
                    {
                        var className = $"{component.Key}{componentPair.Key}".Replace("/", "_").Replace("-", "_");
                        var source = SourceCodeGenerator.GenerateSourceCode(component.Value, className);
                        ctx.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
                    }
                }
            });
        }


        private static string GetDemoName(string filePath)
        {
            return filePath.ToLowerInvariant().Replace(".md", "").Replace(".razor", "").Replace("Demo", "").Replace("_", "");
        }
        private static string GetComponentName(string filePath)
        {
            var parts = filePath.Split('\\');
            var demoIndex = Array.IndexOf(parts, "Demos");
            return $"/{parts[demoIndex + 1]}/{parts[demoIndex + 2]}";
        }

        private static string GetLocale(string filePath)
        {
            return filePath.Split('\\').Last().Replace("index.","").Replace(".md","");
        }
    }
}
