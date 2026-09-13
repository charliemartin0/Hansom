using Helpdesk.Application.Sagas;
using Helpdesk.Domain;
using Microsoft.Extensions.Hosting;
using Hansom;

var builder = Host.CreateApplicationBuilder(args);
builder.UseHansom(options =>
{
    options.HandlerAssemblies.Add(typeof(PlaceOrder).Assembly);
    options.HandlerAssemblies.Add(typeof(OrderSaga).Assembly);
});

var host = builder.Build();
await host.StartAsync();
await host.StopAsync();