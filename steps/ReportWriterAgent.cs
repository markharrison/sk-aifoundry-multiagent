using Azure.AI.Agents.Persistent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using System.ComponentModel;
using SKReportProcess.Events;

namespace SKReportProcess.Steps;

public sealed class ReportWriterAgent(PersistentAgentsClient projectClient, AppSettings setx) : KernelProcessStep
{

    readonly PersistentAgentsClient _projectClient = projectClient;

    [KernelFunction]
    public async ValueTask<string> ExecuteAsync(KernelProcessStepContext context, string threadId)
    {

        Console.WriteLine();
        Console.BackgroundColor = ConsoleColor.Gray;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("[Writer Agent]");
        Console.ResetColor();
        Console.WriteLine();
 
        PersistentAgent definition = await _projectClient.Administration.GetAgentAsync(setx.writerAgentId);
        AzureAIAgent agent = new(definition, _projectClient) { Kernel = new Kernel() };
        AzureAIAgentThread agentThread = new AzureAIAgentThread(_projectClient, threadId);

        KernelPlugin plugin = KernelPluginFactory.CreateFromObject(new Help(_projectClient, context, threadId));
        agent.Kernel.Plugins.Add(plugin);

        await foreach (ChatMessageContent response in agent.InvokeAsync(agentThread))
        {
            Console.Write(response.Content);
        }
        Console.WriteLine();

        return threadId;
    }

    private sealed class Help(PersistentAgentsClient projectClient, KernelProcessStepContext context, string threadId)
    {
        private readonly KernelProcessStepContext _context = context;
        private readonly string _threadId = threadId;
        private readonly PersistentAgentsClient _projectClient = projectClient;

        [KernelFunction("NeedMoreResearch"), Description("Call if you need more research to be performed")]
        async public Task NeedMoreResearchAsync(
            [Description("A description of the research required")] string researchNeeded
        )
        {

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Need more research: ");
            Console.ResetColor();
            Console.WriteLine(researchNeeded);

            await foreach (var run in _projectClient.Runs.GetRunsAsync(_threadId, 1, ListSortOrder.Descending))
            {
                ThreadRun runx = run;

                await _projectClient.Runs.CancelRunAsync(_threadId, runx.Id);

                // poll the thread until the run is cancelled
                while (runx.Status != RunStatus.Cancelled)
                {
                    runx = (await _projectClient.Runs.GetRunAsync(_threadId, runx.Id)).Value;
                    await Task.Delay(1000);
                }

                break; // only the first one
            }

            await _projectClient.Messages.CreateMessageAsync(_threadId,
                    MessageRole.User,
                    "Additional research required: " + researchNeeded
                );

            await _context.EmitEventAsync(new KernelProcessEvent { Id = ProcessEvents.NeedMoreData, Data = _threadId });
        }
    }

}