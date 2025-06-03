using Azure.AI.Agents.Persistent;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;

namespace SKReportProcess.Steps;

public sealed class ReportSenderAgent(PersistentAgentsClient projectClient, AppSettings setx) : KernelProcessStep
{
    readonly PersistentAgentsClient _projectClient = projectClient;

    [KernelFunction]
    public async ValueTask<string> ExecuteAsync(KernelProcessStepContext context, string threadId)
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("[Sender Agent]");
        Console.ResetColor();
        Console.WriteLine();

        PersistentAgent definition = await _projectClient.Administration.GetAgentAsync(setx.senderAgentId);
        AzureAIAgent agent = new(definition, _projectClient) { Kernel = new Kernel() };
        AzureAIAgentThread agentThread = new AzureAIAgentThread(_projectClient, threadId);

        await foreach (ChatMessageContent response in agent.InvokeAsync(agentThread))
        {
            Console.Write(response.Content);
        }
        Console.WriteLine();

        Console.WriteLine("Email sent.");

        return threadId;
    }
}