using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace AntDesign.Docs.Generator
{
    [Generator]
    public class ProgramGenerator : IIncrementalGenerator
    {

        private string Template = """
            namespace SiteGenerator.Sample;
            public class Program
            {
                private static void Main(string[] args)
                {
                    WebApplication.CreateBuilder(args).RunBlazorSite(options =>
                    {
                        options.ConfigComponent(rootComponents => rootComponents.RegisterCustomElements());
                        options.Menus.Add("zh-CN", Menuzh_CN.Data);
                        options.Menus.Add("en-US", Menuen_US.Data);
                    });
                }
            }
            """;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
               "Program.g.cs",
               SourceText.From(Template, Encoding.UTF8)));
        }
    }
}
