using Catalog.Service.Entities;
using Common.MassTransit;
using Common.MongoDB;
using Common.Settings;
using MassTransit;
using MassTransit.Definition;
using Microsoft.OpenApi.Models;

namespace Catalog.Service;
public class Startup
{
    private const string AllowedOriginSetting = "AllowedOrigin";


    private ServiceSettings serviceSettings;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

        services.AddMongo()
                .AddMongoRepository<Item>("items")
                .AddMassTransitWithRabbitMq();

        services.AddControllers(options =>
        {
            // dont remove 'Async' word in method names when building time
            options.SuppressAsyncSuffixInActionNames = false;
        });
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog.Service", Version = "v1" });
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.Service v1"));

            app.UseCors(builder =>
            {
                builder.WithOrigins(Configuration[AllowedOriginSetting])
                        .AllowAnyHeader()
                        .AllowAnyMethod();
            });
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
