using AntDesign.Docs.Generator.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
                .Where(static (text) => text.Path.Contains("\\Demos\\") && text.Path.Contains("index.") && text.Path.EndsWith(".md"))
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
                IList<(string, DemoMenuItem)> docsMenuList = [];
                foreach (var doc in docFiles.GroupBy(static x => x.file.Path.Split('\\').Last().Split('.')[1]))
                {
                    foreach (var item in doc)
                    {
                        Dictionary<string, string> docData = DocParser.ParseHeader(item.content);
                        docsMenuList.Add((doc.Key, new DemoMenuItem()
                        {
                            Order = float.TryParse(docData["order"], out var order) ? order : 0,
                            Title = docData["title"],
                            Url = $"/{doc.Key}/docs/{item.file.Path.Split('\\').Last().Split('.')[0]}",
                            Type = "menuItem"
                        }));
                    }
                }

                var docsMenuI18N = docsMenuList
                    .GroupBy(x => x.Item1)
                    .ToDictionary(x => x.Key, x => x.Select(x => x.Item2));

                var componentDocs = componentFiles.Where(x => x.file.Path.Contains("doc")).GroupBy(static x => GetComponentName(x.file.Path))
                    .Where(x => x.Count() == 2)
                    .SelectMany(o =>
                    {
                        return o.Select(x =>
                        {
                            var language = GetLocale(x.file.Path);
                            (Dictionary<string, string> Meta, string desc, string apiDoc) docData = DocParser.ParseDemoDoc(x.content);

                            return (language, new DemoComponent
                            {
                                Category = docData.Meta["category"],
                                Title = docData.Meta["title"],
                                SubTitle = docData.Meta.TryGetValue("subtitle", out string subtitle) ? subtitle : null,
                                Type = docData.Meta["type"],
                                Desc = docData.desc,
                                ApiDoc = docData.apiDoc.Replace("<h2>API</h2>", $"<h2 id=\"API\"><span>API</span><a href=\"{language}/components/{docData.Meta["title"].ToLower()}#API\" class=\"anchor\">#</a></h2>"),
                                Cols = docData.Meta.TryGetValue("cols", out var cols) ? int.Parse(cols) : (int?)null,
                                Cover = docData.Meta.TryGetValue("cover", out var cover) ? cover : null,
                            });
                        }).ToArray();
                    })
                    .GroupBy(x => x.language)
                    .ToDictionary(x => x.Key, x =>
                    {
                        return x.GroupBy(o => o.Item2.Category)
                            .ToDictionary(k => k.Key, 
                                k => k
                                .GroupBy(j => j.Item2.Type)
                                .Select(j =>
                                {
                                    return new DemoMenuItem()
                                    {
                                        Order = _sortMap[j.Key],
                                        Title = j.Key,
                                        Type = "itemGroup",
                                        Children = j.Select(o => new DemoMenuItem()
                                        {
                                            Title = o.Item2.Title,
                                            SubTitle = o.Item2.SubTitle,
                                            Url = $"{o.Item2.Category}/{o.Item2.Title.ToLowerInvariant()}",
                                            Type = "menuItem",
                                            Cover = o.Item2.Cover,
                                        }).ToArray()
                                    };
                                }).ToArray()
                            );
                    });

                foreach (var lang in new[] { "zh-CN", "en-US" })
                {
                    List<DemoMenuItem> menu = new List<DemoMenuItem>();

                    var children = docsMenuI18N[lang].OrderBy(x => x.Order).ToArray();

                    menu.Add(new DemoMenuItem()
                    {
                        Order = 0,
                        Title = lang == "zh-CN" ? "文档" : "Docs",
                        Type = "subMenu",
                        Url = "docs",
                        Children = children
                    });

                    var categoryComponent = componentDocs[lang];

                    foreach (var component in categoryComponent)
                    {
                        if (!_demoCategoryMap.ContainsKey(component.Key))
                        {
                            continue;
                        }
                        menu.Add(new DemoMenuItem()
                        {
                            Order = Array.IndexOf(_demoCategoryMap.Select(x => x.Key).ToArray(), component.Key) + 1,
                            Title = lang == "zh-CN" ? _demoCategoryMap[component.Key] : component.Key,
                            Type = "subMenu",
                            Url = component.Key.ToLowerInvariant(),
                            Children = component.Value.OrderBy(x => x.Order).ToArray()
                        });
                    }

                    var className = $"Menu{lang}".Replace("/", "_").Replace("-", "_");
                    var source = SourceCodeGenerator.GenerateSourceCode(menu, assembly.AssemblyName, className);

                    ctx.AddSource(className, SourceText.From(source, Encoding.UTF8));
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
            return $"{parts[demoIndex + 1]}/{parts[demoIndex + 2]}";
        }

        private static string GetLocale(string filePath)
        {
            return filePath.Split('\\').Last().Replace("index.", "").Replace(".md", "");
        }
    }
}
