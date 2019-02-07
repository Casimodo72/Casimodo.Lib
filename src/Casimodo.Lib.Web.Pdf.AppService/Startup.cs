using Casimodo.Lib.Web.Pdf.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Casimodo.Lib.WebPdfGenerator
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication("Basic")
                .AddScheme<AuthenticationSchemeOptions, MyBasicAuthHandler>("Basic", null);

            services.AddCors();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // User info to be used with our custom basic authentication.
            services.AddSingleton(x => new MyBasicAuthUserInfo
            {
                // NOTE: Adjust to scenario
                UserName = "[a user name]",
                UserPassword = "[a user password]",
                UserId = "[a dummy user ID]",
            });

            services.AddSingleton(typeof(DinkToPdf.Contracts.IConverter),
                new DinkToPdf.SynchronizedConverter(new DinkToPdf.PdfTools()));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            // NOTE: Adjust to scenario
#if (true)
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
#endif

            app.UseAuthentication();

            // UseHttpsRedirection: Do I need that?
            app.UseHttpsRedirection();

            app.UseMvc();
        }
    }
}
