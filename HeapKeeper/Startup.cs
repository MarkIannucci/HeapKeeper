using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ApplicationInsights.SnapshotCollector;

namespace HeapKeeper
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
            AzureDevOpsOAuthOptions AzDevOpsOptions = new AzureDevOpsOAuthOptions();
            AzDevOpsOptions.ClientId = Configuration["AzureAppId"];
            AzDevOpsOptions.ClientSecret = Configuration["AzureClientSecret"];
            AzDevOpsOptions.Scope.Add("vso.work_write");

            // The following line enables Application Insights telemetry collection.
            services.AddApplicationInsightsTelemetry();
            services.AddSnapshotCollector((configuration) => Configuration.Bind(nameof(SnapshotCollectorConfiguration), configuration));

            services.AddMvc();
            services.AddRazorPages();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = AzureDevOpsAuthenticationDefaults.AuthenticationScheme;
            })

            .AddCookie(options =>
            {
                options.LoginPath = "/signin";
                options.LogoutPath = "/signout";
            })

            .AddAzureDevOps(options =>
            {
                // i couldn't figure out how to use only one of the AzDevOpsOptions objects :(
                options.ClientId = AzDevOpsOptions.ClientId;
                options.ClientSecret = AzDevOpsOptions.ClientSecret;
                options.Scope.Add("vso.work_write");
            });
            services.AddHttpClient<IAzureDevOpsTalkerService, AzureDevOpsTalkerService>();
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
            services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb")).GetAwaiter().GetResult());
            services.AddSingleton<IUserService>(_ => new UserService(Configuration["BasicAuth:UserName"], Configuration["BasicAuth:Password"]));
            IConfigurationSection injectionsSection = Configuration.GetSection("Injections");
            List<Injection> injections = injectionsSection.Get<List<Injection>>();
            services.AddSingleton<ICommentLinkerConfiguration>(_ => new CommentLinkerConfiguration(injections));
            services.AddSingleton<AzureDevOpsOAuthOptions>(AzDevOpsOptions);
            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
            });
        }

        /// <summary>
        /// Creates a Cosmos DB database and a container with the specified partition key. 
        /// https://docs.microsoft.com/en-us/azure/cosmos-db/sql-api-dotnet-application
        /// </summary>
        /// <returns></returns>
        private static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
        {
            string databaseName = configurationSection.GetSection("DatabaseName").Value;
            string containerName = configurationSection.GetSection("ContainerName").Value;
            string account = configurationSection.GetSection("Account").Value;
            string key = configurationSection.GetSection("Key").Value;
            Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient(account, key);
            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
            Microsoft.Azure.Cosmos.DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

            return cosmosDbService;
        }
    }
}
