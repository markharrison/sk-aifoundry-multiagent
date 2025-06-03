using Azure.AI.Agents.Persistent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using System.ComponentModel;
using SKReportProcess.Events;

namespace SKReportProcess.Steps;

public sealed class ReportEditorAgent(PersistentAgentsClient projectClient, AppSettings setx) : KernelProcessStep
{

    readonly PersistentAgentsClient _projectClient = projectClient;

    //readonly AzureAIClientProvider clientProvider = clientProvider;
    //readonly AgentsClient agentsClient = clientProvider.Client.GetAgentsClient();

    [KernelFunction]
    public async ValueTask<string> ExecuteAsync(KernelProcessStepContext context, string threadId)
    {
        Console.WriteLine();
        Console.BackgroundColor = ConsoleColor.Gray;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("[Editor Agent]");
        Console.ResetColor();
        Console.WriteLine();

        PersistentAgent definition = await _projectClient.Administration.GetAgentAsync(setx.editorAgentId);
        AzureAIAgent agent = new(definition, _projectClient) { Kernel = new Kernel() };
        AzureAIAgentThread agentThread = new AzureAIAgentThread(_projectClient, threadId);

        KernelPlugin plugin = KernelPluginFactory.CreateFromObject(new ApprovalTool(_projectClient, context, threadId));
        agent.Kernel.Plugins.Add(plugin);
       
        await foreach (ChatMessageContent response in agent.InvokeAsync(agentThread))
        {
            Console.Write(response.Content);
        }
        Console.WriteLine();

        return threadId;
    }

    private sealed class ApprovalTool(PersistentAgentsClient projectClient, KernelProcessStepContext context, string threadId)
    {
        private readonly KernelProcessStepContext _context = context;
        private readonly string _threadId = threadId;
        private readonly PersistentAgentsClient _projectClient = projectClient;

        [KernelFunction("Rejected")]
        async public Task RejectedAsync(
            [Description("The reason why the report was rejected")] string reason
        )
        {
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

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Rejected: ");
            Console.ResetColor();
            Console.WriteLine(reason);

            await _projectClient.Messages.CreateMessageAsync(_threadId,
                    MessageRole.User,
                    "Rejected: " + reason
                );

            await _projectClient.Messages.CreateMessageAsync(_threadId,
                    MessageRole.User,
                    "Please make the requested edits and resubmit."
                );

            await _context.EmitEventAsync(new KernelProcessEvent { Id = ProcessEvents.NeedsEdit, Data = _threadId });
        }

        [KernelFunction("Approved")]
        async public Task ApprovedAsync(
            [Description("The reason why the report was approved")] string reason
        )
        {
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

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Approved: ");
            Console.ResetColor();
            Console.WriteLine(reason);

            await _context.EmitEventAsync(new KernelProcessEvent { Id = ProcessEvents.Approved, Data = _threadId });
        }
    }

}