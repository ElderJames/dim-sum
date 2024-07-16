using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace AntDesign.Docs.Generator
{
    [Generator]
  public  class ProgramGenerator : IIncrementalGenerator
    {

        private string Template = """
            public class Program
            {
                private static void Main(string[] args)
                {
                    WebApplication.CreateBuilder(args).RunBlazorSite();
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
