using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using QuickHaul.Module.BusinessObjects.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    public class DeliveryEvent : BaseObject
    {
        [RuleRequiredField]
        [ModelDefault("AllowEdit", "False")]
        public virtual DeliveryOrder DeliveryOrder { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual DateTime Timestamp { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual DeliveryOrderStatus? FromStatus { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual DeliveryOrderStatus ToStatus { get; set; }

        [StringLength(100)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string ChangedBy { get; set; }

        [StringLength(500)]
        public virtual string Remarks { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();
            if (Timestamp == default)
                Timestamp = DateTime.UtcNow;
        }
    }
}
