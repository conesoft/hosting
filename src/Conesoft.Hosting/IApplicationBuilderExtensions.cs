﻿using Microsoft.AspNetCore.Builder;
using System.Linq;
using System.Net.Http;

namespace Conesoft.Hosting
{
    public static class IApplicationBuilderExtensions
    {
        static readonly string[] contentTypes = new[] { "text", "json", "xml" };
        public static IApplicationBuilder UseHostingDefaults(this IApplicationBuilder app, bool connectToHost, bool useDefaultFiles, bool useStaticFiles)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                await next.Invoke();
            });

            if(connectToHost)
            {
                app.UsePortReporter(async port =>
                {
                    var args = System.Environment.GetCommandLineArgs();
                    if (args.FirstOrDefault(arg => arg.StartsWith("--conesoft-host-register=")) is string hosting)
                    {
                        var registrationCommand = hosting.Split("=")[1];

                        using var client = new HttpClient();
                        await client.GetAsync($"{registrationCommand}?site={Host.FullDomain}&port={port}");
                    }
                });
            }

            if (useDefaultFiles)
            {
                app.UseDefaultFiles();
            }

            if (useStaticFiles)
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = context =>
                    {
                        if (context.Context.Response.Headers["Content-Type"].Count > 0)
                        {
                            var contentType = context.Context.Response.Headers["Content-Type"][0];
                            if (contentTypes.Any(type => contentType.Contains(type)))
                            {
                                context.Context.Response.Headers["Content-Type"] = contentType + "; charset=utf-8";
                            }
                        }
                        if (context.Context.Response.Headers["Cache-Control"].Count == 0)
                        {
                            context.Context.Response.Headers.Add("Cache-Control", "max-age=31536000, immutable");
                        }
                    }
                });
            }

            app.Use(async (context, next) =>
            {
                if (context.Response.Headers["Cache-Control"].Count == 0)
                {
                    context.Response.Headers.Add("Cache-Control", "no-cache");
                }
                await next.Invoke();
            });
            return app;
        }
    }
}