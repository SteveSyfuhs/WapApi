using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WapAPI.Models
{
    public class Subscription
    {
        public string SubscriptionID { get; set; }
        public string SubscriptionName { get; set; }
        public string AccountAdminLiveEmailId { get; set; }
        public object ServiceAdminLiveEmailId { get; set; }
        public List<object> CoAdminNames { get; set; }
        public List<object> AddOnReferences { get; set; }
        public List<object> AddOns { get; set; }
        public int State { get; set; }
        public int QuotaSyncState { get; set; }
        public int ActivationSyncState { get; set; }
        public string PlanId { get; set; }
        public List<Service> Services { get; set; }
        public object LastErrorMessage { get; set; }
        public object Features { get; set; }
        public string OfferFriendlyName { get; set; }
        public object OfferCategory { get; set; }
        public string Created { get; set; }
    }
}
