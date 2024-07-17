using AntDesign.Docs.Generator.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace AntDesign.Docs.Generator
{
    [Generator]
    public class MenuGenerator : IIncrementalGenerator
    {
        private static readonly Dictionary<string, int> _sortMap = new Dictionary<string, int>()
        {
            ["Docs"] = -2,
            ["文档"] = -2,
            ["Overview"] = -1,
            ["组件总览"] = -1,
            ["General"] = 0,
            ["通用"] = 0,
            ["Layout"] = 1,
            ["布局"] = 1,
            ["Navigation"] = 2,
            ["导航"] = 2,
            ["Data Entry"] = 3,
            ["数据录入"] = 3,
            ["Data Display"] = 4,
            ["数据展示"] = 4,
            ["Feedback"] = 5,
            ["反馈"] = 5,
            ["Localization"] = 6,
            ["Other"] = 7,
            ["其他"] = 7,
            ["Charts"] = 8,
            ["图表"] = 8,
            ["Experimental"] = 9,
            ["高阶功能"] = 9,
        };

        private static readonly Dictionary<string, string> _demoCategoryMap = new Dictionary<string, string>()
        {
            ["Components"] = "组件",
            ["Charts"] = "图表",
            ["Experimental"] = "高阶功能"
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var files = context.AdditionalTextsProvider
                .Where(static (text) => text.Path.Contains("\\Demos\\")&& text.Path.Contains("index.")&&text.Path.EndsWith(".md"))
                .Select(static (text, token) => (file: text, content: text.GetText(token)!.ToString()))
                .Collect();

            var docs = context.AdditionalTextsProvider
               .Where(static (text) => text.Path.Contains("\\docs\\") && text.Path.EndsWith(".md"))
               .Select(static (text, token) => (file: text, content: text.GetText(token)!.ToString()))
               .Collect();


            var compilationAndFiles = files.Combine(docs).Combine(context.CompilationProvider);


            context.RegisterSourceOutput(compilationAndFiles, static (ctx, pair) =>
            {
                var componentFiles = pair.Left.Left;
                var docFiles = pair.Left.Right;
                var assembly = pair.Right;
                IList<Dictionary<string, DemoMenuItem>> docsMenuList = [];
                var componentDocs = componentFiles.Where(x => x.file.Path.Contains("doc")).GroupBy(static x => GetComponentName(x.file.Path))
                    .Where(x => x.Count() == 2).SelectMany(x => x).GroupBy(o => GetLocale(o.file.Path))
                    .ToDictionary(o => o.Key, o =>
                    {
                        var language = o.Key;

                        return o.Select(x =>
                        {
                            (Dictionary<string, string> Meta, string desc, string apiDoc) docData = DocParser.ParseDemoDoc(x.content);

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
                        }).ToList();
                    });

                foreach (var doc in docFiles.GroupBy(static x => x.file.Path.Split('\\').Last().Split('.')[1]))
                {
                    foreach (var item in doc)
                    {
                        Dictionary<string, string> docData = DocParser.ParseHeader(item.content);
                        docsMenuList.Add(new()
                        {
                            [doc.Key] = new DemoMenuItem()
                            {
                                Order = float.TryParse(docData["order"], out var order) ? order : 0,
                                Title = docData["title"],
                                Url = $"docs/{item.file.Path.Split('\\').Last().Split('.')[1]}",
                                Type = "menuItem"
                            }
                        });
                    }
                }

                foreach (var group in componentDocs)
                {
                    Dictionary<string, DemoMenuItem> menu = new Dictionary<string, DemoMenuItem>();

                    foreach (var item in group.Value)
                    {
                        menu.Add(item.Title, new DemoMenuItem()
                        {
                            Order = _sortMap[group.Key],
                            Title = group.Key,
                            Type = "itemGroup",
                            Children = group.Value.Select(x => new DemoMenuItem()
                            {
                                Title = x.Title,
                                SubTitle = x.SubTitle,
                                Url = $"/{x.Title.ToLower()}",
                                Type = "menuItem",
                                Cover = x.Cover,
                            })
                            .OrderBy(x => x.Title)
                            .ToArray(),
                        });

                        docsMenuList.Add(menu);
                    }
                }

                ctx.AddSource("", SourceText.From("", Encoding.UTF8));
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
            return $"{parts[demoIndex + 1]}/{parts[demoIndex + 2]}";
        }

        private static string GetLocale(string filePath)
        {
            return filePath.Split('\\').Last().Replace("index.", "").Replace(".md", "");
        }
    }
}
