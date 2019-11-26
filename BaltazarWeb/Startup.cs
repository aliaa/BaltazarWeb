using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AliaaCommon;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaltazarWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).
                AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                });


            string permissionClaimName = nameof(Permission);
            services.AddAuthorization(options =>
            {
                foreach (string perm in Enum.GetNames(typeof(Permission)))
                    options.AddPolicy(perm, policy => policy.RequireAssertion(context =>
                    {
                        var permClaim = context.User.Claims.FirstOrDefault(c => c.Type == permissionClaimName);
                        return permClaim != null && permClaim.Value.Contains(perm);
                    }));
                options.AddPolicy("Admin", policy => policy.RequireClaim("IsAdmin"));
            });

            services.AddMvc(
                    options => options.ModelBinderProviders.Insert(0, new ModelBinderProvider()))
                .AddJsonOptions(
                    options =>
                    {
                        options.SerializerSettings.Converters.Add(new ObjectIdJsonConverter());
                        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "PersianCharsMap.json");
            IStringNormalizer stringNormalizer = new StringNormalizer(filePath);
            services.AddSingleton(stringNormalizer);

            MongoHelper DB = new MongoHelper(stringNormalizer, Configuration.GetValue<string>("DBName"), Configuration.GetValue<string>("MongoConnString"),
                Configuration.GetValue<bool>("setDictionaryConventionToArrayOfDocuments"), null);
            DB.DefaultWriteLog = false;
            services.AddSingleton(DB);
            services.AddSingleton(new ScoresDataProvider(DB));

            IPushNotificationProvider pusheAPI = new PusheAPI(Configuration.GetValue<string>("PusheToken"), Configuration.GetValue<string>("AndroidPackage"));
            services.AddSingleton(typeof(IPushNotificationProvider), pusheAPI);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
                app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            //    app.UseExceptionHandler("/Home/Error");
            //}

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
