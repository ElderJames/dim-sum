using Microsoft.AspNetCore.Components.Web;

namespace SiteGenerator.Sample
{
    public class Test
    {
        public void Test2()
        {
            WebApplication.CreateBuilder().RunBlazorSite(options =>
            {
                options.RegisterCustomElements();
            });
        }
    }
}
