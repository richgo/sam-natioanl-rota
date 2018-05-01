using Microsoft.WindowsAzure.Storage.Table;

namespace SamaritansNationalRota.Models
{
    public class Branch : TableEntity
    {
        public string BranchName { get { return RowKey; } }
        public string BranchApiKey { get; set; }
    }
}