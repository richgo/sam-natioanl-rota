using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using SamaritansNationalRota.Models;

namespace SamaritansNationalRota
{
    public static class GetBranchRota
    {
        [FunctionName("GetBranchRota")]
        public static async Task<IEnumerable<Shift>> Run([ActivityTrigger] Branch branch, TraceWriter log)
        {
            try
            {
                string jsonRespose = "";
                var today = DateTime.Today;
                var startDate = today.ToString("dd-MM-yyyy");
                var endDate = today.AddDays(60).ToString("dd-MM-yyyy");
                var requestUri = new Uri($"https://3r.org.uk/stats/export_rotas.json?start_date={startDate}&end_date={endDate}");
                var client = new HttpClient();
                var request = new HttpRequestMessage()
                {

                    RequestUri = requestUri,
                    Method = HttpMethod.Get,
                };

                request.Headers.Add("Authorization", $"APIKEY {branch.BranchApiKey}");
                var response = client.SendAsync(request).Result;

                var rotaDetail = new RotaDetail();
                if (response.IsSuccessStatusCode)
                {
                    jsonRespose = await response.Content.ReadAsStringAsync();
                    rotaDetail = JsonConvert.DeserializeObject<RotaDetail>(jsonRespose);
                    foreach (var shift in rotaDetail.shifts)
                    {
                        shift.BranchName = branch.BranchName;
                    }
                    log.Info("Sucessfully ran GetBranchRota for branch: " + branch.BranchName);
                    return rotaDetail.shifts;
                }
                else
                {
                    log.Error("Error when running GetBranchRota for branch: " + branch.BranchName + ". Three Rings API returned: " + response.StatusCode);
                    return null;
                }              
            }
            catch(Exception ex)
            {
                log.Error("Failed to run GetBranchRota for branch:" + branch.BranchName + ".  \n" + ex.ToString());
            }
            return null;
        }
    }
}
