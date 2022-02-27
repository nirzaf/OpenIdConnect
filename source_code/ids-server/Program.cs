#nullable enable
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static idsserver.DataSeeder;

namespace idsserver
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();

            using (IServiceScope serviceScope = host.Services.CreateScope())
            {
                ApplicationDbContext? appDbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
                await appDbContext?.Database.MigrateAsync()!;

                PersistedGrantDbContext? persistentDbContext =
                    serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>();
                await persistentDbContext?.Database.MigrateAsync()!;

                ConfigurationDbContext? configDbContext =
                    serviceScope.ServiceProvider.GetService<ConfigurationDbContext>();
                await configDbContext?.Database.MigrateAsync()!;

                IConfiguration? conf = serviceScope.ServiceProvider.GetService<IConfiguration>();

                if (conf.GetValue("SeedData", true))
                    await SeedIdentityServer(serviceScope.ServiceProvider);
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }
}