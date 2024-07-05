using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog.Extensions.Logging;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Client.Examples
{
    internal class Program
    {
        private static readonly string DemoProcessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "Resources", "demo-net-process.bpmn");
        private static readonly string ZeebeUrl = "localhost:26500";
        private static readonly string ProcessInstanceVariables = "{\"a\":\"123\"}";
        private static readonly string JobType = "net-worker";
        private static readonly string WorkerName = Environment.MachineName;
        private static readonly long WorkCount = 100L;

        public static async Task Main(string[] args)
        {
            // create zeebe client - simple server
            /*var client = ZeebeClient.Builder()
                .UseLoggerFactory(new NLogLoggerFactory())
                .UseGatewayAddress(ZeebeUrl)
                .UsePlainText()
                .Build();
                */
            // create zeebe client - Multi tenancy server, clientid = "modeler", clientSecret= "xxx", audience is "zeebe-api"
            //   authorizationServerUrl: http://localhost:18080/auth/realms/camunda-platform/protocol/openid-connect/token
            // How do I pass that in the connection?
            var client = ZeebeClient.Builder()
                .UseLoggerFactory(new NLogLoggerFactory())
                .UseGatewayAddress(ZeebeUrl)
                .UseTransportEncryption()
                .Build();

            var topology = await client.TopologyRequest()
                .Send();
            Console.WriteLine(topology);
            /*
            await client.NewPublishMessageCommand()
                .MessageName("csharp")
                .CorrelationKey("wow")
                .Variables("{\"realValue\":2}")
                .Send();
*/
            // deploy
            /*
            var deployResponse = await client.NewDeployCommand()
                .AddResourceFile(DemoProcessPath)
                .Send();
*/
            // create process instance
            // var processDefinitionKey = deployResponse.Processes[0].ProcessDefinitionKey;
            var processDefinitionKey = "demo-net-process";

            /*
            var processInstance = await client
                .NewCreateProcessInstanceCommand()
                .BpmnProcessId(processDefinitionKey)
                .Variables(ProcessInstanceVariables)
                .Send();
*/
  /*          await client.NewSetVariablesCommand(processInstance.ProcessInstanceKey).Variables("{\"wow\":\"this\"}").Local().Send();

            for (var i = 0; i < WorkCount; i++)
            {
                await client
                    .NewCreateProcessInstanceCommand()
                    .ProcessDefinitionKey(processDefinitionKey)
                    .Variables(ProcessInstanceVariables)
                    .Send();
            }
*/
            // open job worker
            using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                client.NewWorker()
                      .JobType(JobType)
                      .Handler(HandleJob)
                      .MaxJobsActive(5)
                      .Name(WorkerName)
                      .AutoCompletion()
                      .PollInterval(TimeSpan.FromSeconds(1))
                      .Timeout(TimeSpan.FromSeconds(10))
                      .Open();

                // blocks main thread, so that worker can run
                signal.WaitOne();
            }
        }

        private static void HandleJob(IJobClient jobClient, IJob job)
        {
            // business logic
            var jobKey = job.Key;
            Console.WriteLine("Handling job: " + job);

            if (jobKey % 3 == 0)
            {
                jobClient.NewCompleteJobCommand(jobKey)
                    .Variables("{\"foo\":2}")
                    .Send()
                    .GetAwaiter()
                    .GetResult();
            }
            else if (jobKey % 2 == 0)
            {
                jobClient.NewFailCommand(jobKey)
                    .Retries(job.Retries - 1)
                    .ErrorMessage("Example fail")
                    .Send()
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                // auto completion
            }
        }
    }
}