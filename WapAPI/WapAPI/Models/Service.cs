using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WapAPI.Models
{
    public class Service
    {
        public string Type { get; set; }
        public string State { get; set; }
        public int QuotaSyncState { get; set; }
        public int ActivationSyncState { get; set; }
        public List<BaseQuotaSetting> BaseQuotaSettings { get; set; }
    }
}
