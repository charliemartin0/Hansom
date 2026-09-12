using Helpdesk.Application.Sagas;
using Helpdesk.Domain;
using Microsoft.Extensions.Hosting;
using MiniVerine;

var builder = Host.CreateApplicationBuilder(args);
builder.UseMiniVerine(options =>
{
    options.HandlerAssemblies.Add(typeof(PlaceOrder).Assembly);
    options.HandlerAssemblies.Add(typeof(OrderSaga).Assembly);
});

var host = builder.Build();
await host.StartAsync();
await host.StopAsync();