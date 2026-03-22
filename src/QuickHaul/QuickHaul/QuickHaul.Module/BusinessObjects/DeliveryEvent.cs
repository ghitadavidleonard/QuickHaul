using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using QuickHaul.Module.BusinessObjects.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    public class DeliveryEvent: BaseObject
    {
        [RuleRequiredField]
        public virtual DeliveryOrder DeliveryOrder { get; set; }

        public virtual DateTime Timestamp { get; set; }

        public virtual DeliveryOrderStatus? FromStatus { get; set; }

        public virtual DeliveryOrderStatus ToStatus { get; set; }

        [StringLength(100)]
        public virtual string ChangedBy { get; set; }

        [StringLength(500)]
        public virtual string Remarks { get; set; }
    }
}
