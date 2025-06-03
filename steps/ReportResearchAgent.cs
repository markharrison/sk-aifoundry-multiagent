using Azure;
using Azure.AI.Agents.Persistent;
using Microsoft.SemanticKernel;

namespace SKReportProcess.Steps;


public sealed class ResearchAgent(PersistentAgentsClient projectClient, AppSettings setx) : KernelProcessStep
{
    readonly PersistentAgentsClient _projectClient = projectClient;

    [KernelFunction]
    public async ValueTask<string> ExecuteAsync(KernelProcessStepContext context, string threadId)
    {
        Console.WriteLine();
        Console.BackgroundColor = ConsoleColor.Gray;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("[Researcher Agent]");
        Console.ResetColor();
        Console.WriteLine();

        // SK bug - https://github.com/microsoft/semantic-kernel/issues/12351 
        //
        //PersistentAgent definition = await _projectClient.Administration.GetAgentAsync(setx.researchAgentId);
        //AzureAIAgent agent = new(definition, _projectClient) { Kernel = new Kernel() };
        //AzureAIAgentThread agentThread = new AzureAIAgentThread(_projectClient, threadId);

        //await foreach (ChatMessageContent response in agent.InvokeAsync(agentThread))
        //{
        //    Console.Write(response.Content);
        //}
        //Console.WriteLine();

        ThreadRun run = await _projectClient.Runs.CreateRunAsync(
                threadId,
                setx.researchAgentId);

        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            run = await _projectClient.Runs.GetRunAsync(threadId, run.Id);
        }
        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress);


        AsyncPageable<PersistentThreadMessage> messages = _projectClient.Messages.GetMessagesAsync(threadId: threadId, limit: 1);

        int count = 0;
        await foreach (PersistentThreadMessage threadMessage in messages)
        {

            foreach (MessageContent contentItem in threadMessage.ContentItems)
            {
                if (contentItem is MessageTextContent textItem)
                {
                    Console.Write(textItem.Text);
                }
                else if (contentItem is MessageImageFileContent imageFileItem)
                {
                    Console.Write($"<image from ID: {imageFileItem.FileId}");
                }
                Console.WriteLine();
            }

            if (count++ > 0)   // bug - not currently limiting to 1 message
                break;

        }

        return threadId;
    }
}