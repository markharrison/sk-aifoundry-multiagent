using Microsoft.SemanticKernel;
using Azure.AI.Projects;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Azure.AI.Agents.Persistent;

namespace SKReportProcess.Steps;

public sealed class Init(PersistentAgentsClient projectClient, AppSettings setx ) : KernelProcessStep
{
    readonly PersistentAgentsClient _projectClient = projectClient;

    [KernelFunction]
    public async ValueTask<string> ExecuteAsync(KernelProcessStepContext context, string reportTopic)
    {
        // Create the Azure provider. (Implement GetAzureProvider with your settings.)
        //AgentsClient client = clientProvider.Client.GetAgentsClient();


        await foreach (var agentx in  _projectClient.Administration.GetAgentsAsync())
        {
            if (agentx.Name.StartsWith("Report-"))
            {
                Console.WriteLine($"Agent: {agentx.Id} - {agentx.Name}");
            }
        }        

        try
        {
            PersistentAgent agent = await _projectClient.Administration.GetAgentAsync(setx.researchAgentId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get agent {setx.researchAgentId}: {ex.Message}");
            Environment.Exit(-1);
        }


        PersistentAgentThread thread = await _projectClient.Threads.CreateThreadAsync();

        PersistentThreadMessage message = await _projectClient.Messages.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                reportTopic);

        return thread.Id;

    }
}