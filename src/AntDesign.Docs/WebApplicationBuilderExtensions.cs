using AntDesign.Docs.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    public static class WebApplicationBuilderExtensions
    {
        public static void RunBlazorSite(this WebApplicationBuilder builder, Action<IJSComponentConfiguration> action)
        {
            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents(options =>
                {
                    action.Invoke(options.RootComponents);
                });

            builder.Services.AddAntDesignDocs();

            var app = builder.Build();

            app.Services.GetRequiredService<AssemblyService>().AddAssembly(Assembly.GetEntryAssembly());

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<AntDesign.Docs.App>()
                .AddAdditionalAssemblies(Assembly.GetEntryAssembly())
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
