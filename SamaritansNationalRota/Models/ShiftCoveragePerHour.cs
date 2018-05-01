using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SamaritansNationalRota.Models
{
    public class ShiftCoveragePerHour
    {
        public string Branch { get; set; }
        public DateTime StartDate { get; set; }
        public string StartHour { get; set; }
        public DateTime EndDate { get; set; }
        public int VolunteerCount { get; set; }
        public string ShiftName { get; internal set; }

        public IEnumerable<Volunteer> Volunteers { get; set; }
    }
}
