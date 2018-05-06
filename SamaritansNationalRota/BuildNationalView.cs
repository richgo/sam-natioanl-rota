using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SamaritansNationalRota.Models;

namespace SamaritansNationalRota
{
    public static class BuildNationalView
    {
        [FunctionName("BuildNationalView")]
        public static async Task Run([OrchestrationTrigger] DurableOrchestrationContext ctx, [Table("RotaView", Connection = "SamaritansRotaConnectionString")]CloudTable rotaViewTable, [Table("RotaConfig", "Branches", Connection = "SamaritansRotaConnectionString")] IQueryable<Branch> branchQuery, TraceWriter log)
        {
            try
            {
                var parallelTasks = new List<Task<RotaDetail>>();

                var branches = branchQuery.ToList();

                if (branches == null || branches.Count() == 0)
                {
                    log.Warning("No Branches configured in the RotaConfig table, exiting");
                    return;
                }

                foreach (var branch in branches)
                {
                    Task<RotaDetail> task = ctx.CallActivityAsync<RotaDetail>("GetBranchRota", branch);
                    parallelTasks.Add(task);
                }

                await Task.WhenAll(parallelTasks);

                var allShifts = parallelTasks.Where(t => t.Result != null && t.Result.shifts != null)
                                             .SelectMany(t => t.Result.shifts);
                

                // Comment this out to insert test data into the cloud table (i.e. when there are no working branch keys)
                if (!allShifts.Any())
                {
                    log.Error("No Rota data returned for any Branches.");                    
                }

                // Persist the shifts in a cloud table per day

                var shiftCoveragePerHours = GetShiftsOnHourByHourBasis(allShifts.ToList()).ToList();

                var distinctDays = shiftCoveragePerHours.Select(s => s.StartDate.Date)
                                                        .Distinct();
                foreach(var day in distinctDays)
                {
                    var shiftsForDay = shiftCoveragePerHours.Where(s => s.StartDate.Date == day.Date)
                                                            .OrderBy(s => s.StartDate);

                    var rotaViewForDay = new RotaView
                    {
                        PartitionKey = "RotaView",
                        RowKey = day.ToString("dd-MM-yyyy"),
                        ShiftCoverage = JsonConvert.SerializeObject(shiftsForDay)
                    };
                    
                    var insertOrReplaceOperation = TableOperation.InsertOrReplace(rotaViewForDay);
                    rotaViewTable.Execute(insertOrReplaceOperation);
                }

            }
            catch(Exception ex)
            {
                log.Error("Failed to run BuildNationalView. " + ex.ToString());
            }
        }

        private static IEnumerable<ShiftCoveragePerHour> GetShiftsOnHourByHourBasis(IList<Shift> shifts)
        {
            List<ShiftCoveragePerHour> shiftCoveragePerHour = new List<ShiftCoveragePerHour>();

            #region generate some test data
            // comment this in to generate some test data.

            //for(int i = 0;i <20;i++)
            //{
            //    int day = 0;
            //    if(i % 4 == 0)
            //    {
            //        day++;
            //    }
            //    var testShift = new Shift
            //    {
            //        BranchName = "test branch " + i,
            //        rota = "test rota name " + i,
            //        start_datetime = DateTime.Now.AddDays(day),
            //        duration = 1000,
            //        volunteers = new Volunteer[0],
            //        id = i,
            //        title = "Test title " + i
            //    };
            //    shifts.Add(testShift);
            //}
            #endregion

            foreach (Shift shift in shifts)
            {
                DateTime startDate = shift.start_datetime;
                DateTime endDate = shift.end_datetime;

                while (startDate < endDate)
                {
                    ShiftCoveragePerHour shiftCoverage = new ShiftCoveragePerHour
                    {
                        Branch = shift.BranchName,
                        ShiftName = shift.rota,
                        StartDate = shift.start_datetime,
                        EndDate = shift.end_datetime,
                        StartHour = startDate.ToString("HH:mm"),
                        Volunteers = shift.volunteers,
                        VolunteerCount = shift.volunteers.Count()
                    };
                    shiftCoveragePerHour.Add(shiftCoverage);

                    startDate = startDate.AddMinutes(30);
                }
            }

            return shiftCoveragePerHour.OrderBy(x => x.Branch)
                .ThenBy(x => x.StartDate)
                .ThenBy(x => x.StartHour)
                .ThenBy(x => x.ShiftName);

        }
    }
}
