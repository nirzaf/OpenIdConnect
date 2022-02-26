using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using weatherapi.Controllers;

namespace weatherapi
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
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Audience = "weatherapi";
                    var issuer = Configuration.GetValue<string>("IDSServer");

                    if (issuer.StartsWith("http"))
                        options.Authority = issuer; // Issued by identity server
                    else
                    {
                        // this is self issued
                        var signKey = Configuration.GetValue<string>("IssuerSigningKey");
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey)),
                            ValidateIssuer = true,
                            ValidIssuer = issuer,
                            SaveSigninToken = true
                        };
                    }
                });

            services.AddControllers()
                // see https://github.com/ddubson/contract-testing-dotnetcore-example#-gotchas
                .AddApplicationPart(typeof(WeatherForecastController).Assembly);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "weatherapi", Version = "v1" });
            });

            // add CORS
            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "weatherapi v1"));
            }

            app.UseHttpsRedirection();

            // use Cors
            app.UseCors(config => config
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
            );

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}