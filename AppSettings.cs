using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


namespace SKReportProcess
{
    public class AppSettings
    {
        public string afProjectConnectionString { get; set; }
        public string afProjectEndpoint { get; set; }

        public string editorAgentId { get; set; }
        public string researchAgentId { get; set; }

        public string writerAgentId { get; set; }
        public string senderAgentId { get; set; }


        public ConfigurationManager configuration;

        public AppSettings()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development";
            var hostBuilder = Host.CreateApplicationBuilder();
            hostBuilder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables();

            configuration = hostBuilder.Configuration;

            afProjectConnectionString = configuration["ProjectConnectionString"] ?? "";
            afProjectEndpoint = configuration["ProjectEndpoint"] ?? "";
            editorAgentId = configuration["EditorAgentId"] ?? "";
            researchAgentId = configuration["ResearchAgentId"] ?? "";
            writerAgentId = configuration["WriterAgentId"] ?? "";
            senderAgentId = configuration["SenderAgentId"] ?? "";


        }

    }
}

