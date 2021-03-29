using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Runtime.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace whoSkill
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddBotRuntime(this.Configuration);

            // The workaround below will be removed with next SDK patch, when this bug fix gets released: https://github.com/microsoft/botbuilder-dotnet/issues/5239
            // In the meantime, we're guaranteed to have CoreAdapter registered as IBotFrameworkHttpAdapter, so look it up and register it as BotAdapter.
            RegisterCoreBotAdapter(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#pragma warning disable CA1801 // Review unused parameters
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
#pragma warning restore CA1801 // Review unused parameters
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseRouting()
               .UseEndpoints(endpoints =>
               {
                   endpoints.MapControllers();
               });
        }

        private static void RegisterCoreBotAdapter(IServiceCollection services)
        {
            const string coreBotAdapterName = "CoreBotAdapter";

            services.AddSingleton(sp =>
            {
                var adapters = sp.GetServices<IBotFrameworkHttpAdapter>();
                return (BotAdapter)adapters.Single(a => typeof(BotAdapter).IsAssignableFrom(a.GetType()) && a.GetType().Name.Contains(coreBotAdapterName));
            });
        }
    }
}
