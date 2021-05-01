using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace idsserver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var appdbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
                appdbContext.Database.Migrate();

                var persistentdbContext = serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>();
                persistentdbContext.Database.Migrate();

                var configDbContext = serviceScope.ServiceProvider.GetService<ConfigurationDbContext>();
                configDbContext.Database.Migrate();

                var conf = serviceScope.ServiceProvider.GetService<IConfiguration>();
                if (conf.GetValue("SeedData", true))
                    DataSeeder.SeedIdentityServer(serviceScope.ServiceProvider);
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
