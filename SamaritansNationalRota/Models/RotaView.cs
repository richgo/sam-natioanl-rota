using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamaritansNationalRota.Models
{
    public class RotaView : TableEntity
    {       
        public string ShiftCoverage { get; set; }     
    }
}
