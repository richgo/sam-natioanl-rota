using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using SamaritansNationalRota.Models;

namespace SamaritansNationalRota
{
    public static class BuildNationalView
    {
        [FunctionName("BuildNationalView")]
        [return: Table("RotaView", Connection = "SamaritansRotaConnectionString")]
        public static async Task<RotaView> RunScheduled([OrchestrationTrigger] DurableOrchestrationContext ctx, [Table("RotaConfig", "Branches", Connection = "SamaritansRotaConnectionString")] IQueryable<Branch> branchQuery, TraceWriter log)
        {
            try
            {
                var parallelTasks = new List<Task<RotaDetail>>();

                var branches = branchQuery.ToList();

                if (branches == null || branches.Count() == 0)
                {
                    log.Warning("No Branches configured in the RotaConfig table, exiting");
                    return null;
                }

                foreach (var branch in branches)
                {
                    Task<RotaDetail> task = ctx.CallActivityAsync<RotaDetail>("GetBranchRota", branch);
                    parallelTasks.Add(task);
                }

                await Task.WhenAll(parallelTasks);

                var allShifts = parallelTasks.Where(t => t.Result != null && t.Result.shifts != null)
                                             .SelectMany(t => t.Result.shifts);
                
                if (!allShifts.Any())
                {
                    log.Error("No Rota data returned for any Branches.");
                    return null;
                }

                var shiftCoveragePerHours = GetShiftsOnHourByHourBasis(allShifts.ToList()).ToList();

                return new RotaView { PartitionKey = "RotaView", RowKey = "RotaView", ShiftCoverage = shiftCoveragePerHours };
            }
            catch(Exception ex)
            {
                log.Error("Failed to run BuildNationalView. " + ex.ToString());
            }
            return null;
        }

        private static IEnumerable<ShiftCoveragePerHour> GetShiftsOnHourByHourBasis(IEnumerable<Shift> shifts)
        {
            List<ShiftCoveragePerHour> shiftCoveragePerHour = new List<ShiftCoveragePerHour>();

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
