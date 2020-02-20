using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;


namespace Boyd.DataBuses.Tests
{
    public class SignalREchoServer
    {
        private readonly IWebHost host;
        public SignalREchoServer(string bindUrl)
        {
            host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(bindUrl)
                .UseStartup<SignalREchoServerApp>()
                .Build();
            host.Start();
        }

        public async Task Stop()
        {
        
            await host.StopAsync();
        }
    }

    public class EchoHub : Hub
    {
        public async Task echo(byte[] bytes)
        {
            await Clients.Caller.SendAsync("echo", bytes);
        }

        public async Task echoObject(TestMPackMessage message)
        {
            await Clients.Caller.SendAsync("echoObject", message);
        }
    }
    
    public class SignalREchoServerApp
    {

        public SignalREchoServerApp(IConfiguration pConfig)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR()
                .AddJsonProtocol();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(builder => builder.MapHub<EchoHub>("/echo"));
        }
    }
    
}