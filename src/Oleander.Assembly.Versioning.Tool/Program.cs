using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Tool.Options;
using Oleander.Extensions.DependencyInjection;
using Oleander.Extensions.Hosting.Abstractions;
using Oleander.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;
using Oleander.Extensions.Logging.Providers;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Oleander.Assembly.Versioning.Tool.Commands;

namespace Oleander.Assembly.Versioning.Tool
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration
                .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"), true, false);

            builder.Services
                .AddSingleton<AssemblyVersioningTool>()
                .AddConfiguredTypes("loggerTypes");

            builder.Logging
                .ClearProviders()
                .AddConfiguration(builder.Configuration.GetSection("Logging"))
                .Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LoggerSinkProvider>());

            var host = builder.Build();
            host.Services.InitLoggerFactory();


            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
            var console = new ToolConsole(logger);
            var tool = host.Services.GetRequiredService<AssemblyVersioningTool>();
            
            
            var rootCommand = new RootCommand("assembly-versioning-tool");
            var commandLine = new CommandLineBuilder(rootCommand)
                .UseDefaults() // automatically configures dotnet-suggest
                .Build();

            TabCompletions.Logger = logger;

            rootCommand.AddCommand(new UpdateAssemblyVersionCommand(logger, tool));
            rootCommand.AddCommand(new CompareAssemblyCommand(logger, tool));

            var exitCode = await commandLine.InvokeAsync(args, console);

            console.Flush();

            const string logMsg = "assembly-versioning '{args}' exit with exit code {exitCode}";

            var arguments = string.Join(" ", args);

            if (exitCode == 0)
            {
                logger.LogInformation(logMsg, arguments, exitCode);

                if (!arguments.StartsWith("[suggest:"))
                {
                    MSBuildLogFormatter.CreateMSBuildMessage("AVT0", $"assembly-versioning {exitCode}", "Main");
                }
            }
            else
            {
                logger.LogError(logMsg, arguments, exitCode);
            }

            await host.LogConfiguredTypesExceptions<Program>(true).WaitForLoggingAsync(TimeSpan.FromSeconds(5));
            return exitCode;
        }
    }
}