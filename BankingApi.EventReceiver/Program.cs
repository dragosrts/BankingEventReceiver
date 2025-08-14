using Azure.Messaging.ServiceBus;
using BankingApi.EventReceiver;
using BankingApi.EventReceiver.Infrastructure;
using BankingApi.EventReceiver.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
public class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        // In Program.cs (minimal hosting)
        builder.Services.AddDbContext<BankingApiDbContext>(opts =>
            opts.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

        builder.Services.AddScoped<IBankAccountService, BankAccountService>();

        builder.Services.AddSingleton(sp =>
        {
            var conn = builder.Configuration.GetConnectionString("ServiceBus");
            return new ServiceBusClient(conn);
        });

        builder.Services.AddSingleton<IServiceBusReceiver>(sp =>
        {
            var client = sp.GetRequiredService<ServiceBusClient>();
            var queueName = builder.Configuration["ServiceBus:Queue"]!;
            return new MessageReceiver(client, queueName);
        });

        builder.Services.AddHostedService<WorkerHostedService>();
    }

    public sealed class WorkerHostedService : BackgroundService
    {
        private readonly MessageWorker _worker;
        public WorkerHostedService(MessageWorker worker) => _worker = worker;

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => _worker.RunAsync(stoppingToken);
    }
}
