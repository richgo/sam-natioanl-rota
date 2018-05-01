using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace SamaritansNationalRota
{
    public static class TriggerBuildNationalView
    {
        [FunctionName("TriggerBuildNationalView")]
        public static async Task RunAsync([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, [OrchestrationClient] DurableOrchestrationClient starter, TraceWriter log)
        {

            string functionName = "BuildNationalView";
            string instanceId = await starter.StartNewAsync(functionName, null);
            
            log.Info($"Triggered BuildNationalView at: {DateTime.Now}");
        }
    }
}
