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
    public class DocsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var pipeline = context.AdditionalTextsProvider
                .Where(static (text) => text.Path.Contains("\\docs\\") && text.Path.EndsWith(".md"))
                .Select(static (text, cancellationToken) =>
                {
                    var fileNameParts = Path.GetFileNameWithoutExtension(text.Path).Split('.');
                    var name = fileNameParts[0];
                    var locale = fileNameParts[1];
                    var html = GeneratorPages(name, locale, text.GetText(cancellationToken)!.ToString());
                    return (name, locale, html);
                });

            context.RegisterSourceOutput(pipeline,
                static (context, pair) =>
                    // Note: this AddSource is simplified. You will likely want to include the path in the name of the file to avoid
                    // issues with duplicate file names in different paths in the same project.
                    context.AddSource($"{pair.locale}/{pair.name}.g.cs", SourceText.From(pair.html, Encoding.UTF8)));
        }

        private static string GeneratorPages(string name, string locale, string text)
        {
            var doc = DocParser.ParseDocs(text);
            var template = """
                using Microsoft.AspNetCore.Components;
                using Microsoft.AspNetCore.Components.Rendering;

                namespace AntDesign.Docs.Demos.Pages;

                [Route("{{url}}")]
                public class {{className}}Component : ComponentBase
                {
                    string html =
                {{html}};
                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddMarkupContent(0, html);
                    }
                }
                
                """;

            return template.Replace("{{html}}", $"\"\"\"\r\n{doc.html}\r\n\"\"\"")
                .Replace("{{url}}", $"/{locale}/docs/{name}")
                .Replace("{{className}}", $"{name}{locale}".ToPascalCase());
        }
    }
}
