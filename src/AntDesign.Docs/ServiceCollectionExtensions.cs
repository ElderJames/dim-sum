using System.Net.Http;
using System;
using System.Reflection;
using AntDesign.Docs.Highlight;
using AntDesign.Docs.Services;
using AntDesign.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAntDesignDocs(this IServiceCollection services)
        {
            services.AddAntDesign();
            services.AddScoped<DemoService>();
            //services.AddScoped<IPrismHighlighter, PrismHighlighter>();
            services.AddSingleton<AssemblyService>();

            services.AddSimpleEmbeddedJsonLocalization(options =>
            {
                options.ResourcesPath = "Resources";
                options.Resources = SimpleStringLocalizerOptions.BuildResources("Resources", Assembly.GetExecutingAssembly());
            });

            //services.AddSimpleInteractiveStringLocalizer();
            services.AddInteractiveStringLocalizer();
            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            //services.AddJsonLocalization(b =>
            //{
            //    b.UseEmbeddedJson(o => o.ResourcesPath = "Resources");
            //}, ServiceLifetime.Singleton);
            //services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddSingleton(sp =>
            {
                var httpContext = sp.GetService<IHttpContextAccessor>()?.HttpContext;
                if (httpContext != null)
                {
                    var request = httpContext.Request;
                    var host = request.Host.ToUriComponent();
                    var scheme = request.Scheme;
                    var baseAddress = $"{scheme}://{host}";
                    return new HttpClient() { BaseAddress = new Uri(baseAddress) };
                }
                else
                {
                    return new HttpClient() { BaseAddress = new Uri("http://0.0.0.0:8181") };
                }
            });

            return services;
        }
    }
}
