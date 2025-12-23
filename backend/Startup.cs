using System;
using Backend.Const;
using Backend.DataAccess.DBcontexts;
using Backend.Helper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;
using StackExchange.Redis;

namespace Backend
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ReferenceLoopHandling =
                        Newtonsoft.Json.ReferenceLoopHandling.Ignore);
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen();


            var appConnection = Configuration.GetConnectionString("AppConnection");
            services.AddDbContext<StoreContext>(options => options.UseSqlServer(appConnection));

            var minIoConnection = Configuration.GetSection("minioConnection");

            services.Configure<MinioOptions>(Configuration.GetSection("minioConnection"));
            services.Configure<RedisOptions>(Configuration.GetSection("Redis"));

            services.AddSingleton(new Snowflake(1, 0));


            services.AddSingleton<IMinioClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
                return new MinioClient()
                 .WithEndpoint(options.MinioEndpoint)
                 .WithCredentials(options.MinioAccessKey, options.MinioSecretKey)
                 .WithSSL(options.UseSSL)
                 .Build();
            });

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    throw new InvalidOperationException("Redis connection string is not configured.");
                }

                var configuration = ConfigurationOptions.Parse(options.ConnectionString, true);
                if (options.Database.HasValue)
                {
                    configuration.DefaultDatabase = options.Database.Value;
                }

                return ConnectionMultiplexer.Connect(configuration);
            });

            services.AddSingleton<RedisService>();


        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
