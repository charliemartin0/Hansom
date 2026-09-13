using Helpdesk.Application.Sagas;
using Helpdesk.Domain;
using Helpdesk.Infrastructure;
using Microsoft.Extensions.Hosting;
using Hansom;

var builder = Host.CreateApplicationBuilder(args);
builder.UseHansom(options =>
{
    options.HandlerAssemblies.Add(typeof(PlaceOrder).Assembly);
    options.HandlerAssemblies.Add(typeof(OrderSaga).Assembly);
});

string? connectionString = Environment.GetEnvironmentVariable("HANSOM_PG_CONNECTIONSTRING");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    await builder.UseHelpdeskPostgres(connectionString);
}

var host = builder.Build();
await host.StartAsync();
await host.StopAsync();