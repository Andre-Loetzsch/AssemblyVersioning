using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Tool.Commands;
using Oleander.Assembly.Versioning.Tool.Options;
using Oleander.Extensions.DependencyInjection;
using Oleander.Extensions.Hosting.Abstractions;
using Oleander.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;
using Oleander.Extensions.Logging.Providers;
using System.CommandLine;

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
                .AddSingleton<CompareAssembliesTool>()
                .AddConfiguredTypes("loggerTypes");

            builder.Logging
                .ClearProviders()
                .AddConfiguration(builder.Configuration.GetSection("Logging"))
                .Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LoggerSinkProvider>());

            var host = builder.Build();
            host.Services.InitLoggerFactory();

            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
            var assemblyVersioningTool = host.Services.GetRequiredService<AssemblyVersioningTool>();
            var compareAssembliesTool = host.Services.GetRequiredService<CompareAssembliesTool>();
            var rootCommand = new RootCommand("assembly-versioning-tool");

            TabCompletions.Logger = logger;

            rootCommand.Add(new UpdateAssemblyVersionCommand(logger, assemblyVersioningTool));
            rootCommand.Add(new CompareAssembliesCommand(logger, compareAssembliesTool));

            var outWriter = new StringWriter();
            var errorWriter = new StringWriter();
            var exitCode = await rootCommand.Parse(args).InvokeAsync(new()
            {
                Output = outWriter,
                Error = errorWriter
            });

            var outText = outWriter.ToString();
            var errorText = errorWriter.ToString();

            if (!string.IsNullOrEmpty(errorText))
            {
                logger.LogError("{stream.error}", errorText);
                Console.WriteLine(MSBuildLogFormatter.CreateMSBuildErrorFormat("SRG1", outText, "Oleander.StrResGen.Tool"));
            }
            else
            {
                logger.LogInformation("{stream.out}", outText);
            }

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