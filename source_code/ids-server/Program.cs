using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace idsserver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();

            using (IServiceScope serviceScope = host.Services.CreateScope())
            {
                ApplicationDbContext? appdbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
                appdbContext.Database.Migrate();

                PersistedGrantDbContext? persistentdbContext =
                    serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>();
                persistentdbContext.Database.Migrate();

                ConfigurationDbContext? configDbContext =
                    serviceScope.ServiceProvider.GetService<ConfigurationDbContext>();
                configDbContext.Database.Migrate();

                IConfiguration? conf = serviceScope.ServiceProvider.GetService<IConfiguration>();
                if (conf.GetValue("SeedData", true))
                    DataSeeder.SeedIdentityServer(serviceScope.ServiceProvider);
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }
}