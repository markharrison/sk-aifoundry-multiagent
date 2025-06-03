using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using SKReportProcess.Steps;
using SKReportProcess.Events;
using SKReportProcess.Processes;  


namespace SKReportProcess
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
       
            AppSettings setx = new();

            PersistentAgentsClient projectClient = new(setx.afProjectEndpoint, new DefaultAzureCredential());

            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.Services.AddSingleton(projectClient);
            kernelBuilder.Services.AddSingleton(setx);
            Kernel kernel = kernelBuilder.Build();

            // Create a process 
            KernelProcess kernelProcess = ProcessFactory.BuildReportProcess();

            Console.Clear();
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("What would you like a report on?");
            Console.ResetColor();
            Console.Write(": ");
            string input = Console.ReadLine()!;

            // Start the process with an initial external event
            await using var runningProcess = await kernelProcess.StartAsync(
                kernel,
                    new KernelProcessEvent()
                    {
                        Id = ProcessEvents.StartProcess,
                        Data = input
                    });

            Environment.Exit(0);

        }


    }
}
