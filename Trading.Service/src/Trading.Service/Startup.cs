using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Identity;
using Common.MassTransit;
using Common.MongoDB;
using Common.Settings;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Trading.Service.StateMachines;
using System.Text.Json.Serialization;
using Trading.Service.Entities;
using System.Reflection;
using GreenPipes;
using Trading.Service.Exceptions;
using Trading.Service.Settings;
using Inventory.Service;
using Play.Identity.Contracts;
using Microsoft.AspNetCore.SignalR;
using Play.Trading.Service.SignalR;
using Trading.Service.SignalR;

namespace Trading.Service;
public class Startup
{
    private const string AllowedOriginSetting = "AllowedOrigin";

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMongo()
                .AddMongoRepository<CatalogItem>("catalogitems")
                .AddMongoRepository<InventoryItem>("inventoryitems")
                .AddMongoRepository<ApplicationUser>("users")
                .AddJwtBearerAuthentication();

        AddMassTransit(services);

        services.AddControllers(options =>
        {
            // part 100 time 11:50
            options.SuppressAsyncSuffixInActionNames = false;
        })
        .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trading.Service", Version = "v1" });
        });

        services.AddSingleton<IUserIdProvider, UserIdProvider>()
                    .AddSingleton<MessageHub>()
                    .AddSignalR();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading.Service v1"));

            app.UseCors(builder =>
            {
                builder.WithOrigins(Configuration[AllowedOriginSetting])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<MessageHub>("/messagehub");
        });
    }

    private void AddMassTransit(IServiceCollection services)
    {
        services.AddMassTransit(configure =>
        {
            configure.UsingPlayEconomyRabbitMq(retryConfigurator =>
            {
                retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                retryConfigurator.Ignore(typeof(UnknownItemException));
            });
            configure.AddConsumers(Assembly.GetEntryAssembly());
            configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>(sagaConfigurator =>
            {
                sagaConfigurator.UseInMemoryOutbox();
            })
                    .MongoDbRepository(r =>
                    {
                        var serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                        var mongoSettings = Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();

                        r.Connection = mongoSettings.ConnectionString;
                        r.DatabaseName = serviceSettings.ServiceName;
                    });
        });

        var queueSettings = Configuration.GetSection(nameof(QueueSettings)).Get<QueueSettings>();

        EndpointConvention.Map<GrantItems>(new Uri(queueSettings.GrantItemsQueueAddress));
        EndpointConvention.Map<DebitGil>(new Uri(queueSettings.DebitGilQueueAddress));
        EndpointConvention.Map<SubtractItems>(new Uri(queueSettings.SubtractItemsQueueAddress));

        services.AddMassTransitHostedService();
        services.AddGenericRequestClient();
    }
}
