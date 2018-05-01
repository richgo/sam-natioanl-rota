using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamaritansNationalRota.Models
{
    public class RotaView
    {       
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public List<ShiftCoveragePerHour> ShiftCoverage { get; set; }     
    }
}
