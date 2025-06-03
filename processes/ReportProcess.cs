using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using SKReportProcess.Events;
using SKReportProcess.Steps;

namespace SKReportProcess.Processes
{
    public static class ProcessFactory
    {
        public static KernelProcess BuildReportProcess()
        {
            // Create a process that will interact with the chat completion service
            ProcessBuilder process = new("ChatBot");
            var init = process.AddStepFromType<Init>();
            var researcher = process.AddStepFromType<ResearchAgent>();
            var writer = process.AddStepFromType<ReportWriterAgent>();
            var editor = process.AddStepFromType<ReportEditorAgent>();
            var sender = process.AddStepFromType<ReportSenderAgent>();

            // Define the process flow
            process
                .OnInputEvent(ProcessEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(init));

            init
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(researcher));

            researcher
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(writer));

            writer
                .OnEvent(ProcessEvents.NeedMoreData)
                .SendEventTo(new ProcessFunctionTargetBuilder(researcher));

            writer
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(editor));

            editor
                .OnEvent(ProcessEvents.NeedsEdit)
                .SendEventTo(new ProcessFunctionTargetBuilder(writer));

            editor
                .OnEvent(ProcessEvents.Approved)
                .SendEventTo(new ProcessFunctionTargetBuilder(sender));

            sender
                .OnFunctionResult()
                .StopProcess();

            // Build the process to get a handle that can be started
            return process.Build();
        }
    }
}
