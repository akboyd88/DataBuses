﻿using System.Collections.Generic;
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
        private IWebHost host;
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
        private IConfiguration config;

        public SignalREchoServerApp(IConfiguration pConfig)
        {
            config = pConfig;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR()
                .AddJsonProtocol();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSignalR((builder => builder.MapHub<EchoHub>("/echo")));
        }
    }
    
}