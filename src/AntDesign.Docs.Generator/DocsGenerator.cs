// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace AntDesign.Docs.Generator
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var pipeline = context.AdditionalTextsProvider
                .Where(static (text) => text.Path.EndsWith(".md"))
                .Select(static (text, cancellationToken) =>
                {
                    var name = Path.GetFileName(text.Path);
                    var code = GeneratorPages(name, text.GetText(cancellationToken)!.ToString());
                    return (name, code);
                });

            context.RegisterSourceOutput(pipeline,
                static (context, pair) =>
                    // Note: this AddSource is simplified. You will likely want to include the path in the name of the file to avoid
                    // issues with duplicate file names in different paths in the same project.
                    context.AddSource($"{pair.name}generated.cs", SourceText.From(pair.code, Encoding.UTF8)));
        }

        private static string GeneratorPages(string name, string text)
        {
            var html = Markdown.ToHtml(text);
            var template = """
                                
                namespace AntDesign.Docs.Demos.Pages;

                [Route("/docs/{{name}}")]
                public class {{name}}Component : ComponentBase
                {
                    string html = {{html}};
                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddMarkupContent(0, html);
                    }
                }
                
                """;

            return template.Replace("{{html}}", $"\"\"\"{html}\"\"\"")
                .Replace("{{name}})", name);
        }
    }
}
