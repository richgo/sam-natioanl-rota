using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamaritansNationalRota.Models
{
    public class Shift
    {
        public int id { get; set; }
        public string rota { get; set; }
        public string title { get; set; }
        public DateTime start_datetime { get; set; }

        public DateTime end_datetime { get { return start_datetime.AddSeconds(duration); } }
        public int duration { get; set; }
        public Volunteer[] volunteers { get; set; }

        public int CountOfVolunteer { get { return volunteers.Count(); } }

        public string BranchName { get; set; }
    }
}
