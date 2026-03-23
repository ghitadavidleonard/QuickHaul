using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using QuickHaul.Module.BusinessObjects.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class DeliveryOrder : BaseObject
    {
        [ModelDefault("AllowEdit", "False")]
        [StringLength(20)]
        public virtual string OrderNumber { get; set; }

        [RuleRequiredField]
        public virtual Customer Customer { get; set; }

        [RuleRequiredField]
        [StringLength(300)]
        [ModelDefault("RowCount", "3")]
        public virtual string PickupAddress { get; set; }

        [RuleRequiredField]
        [StringLength(300)]
        [ModelDefault("RowCount", "3")]
        public virtual string DeliveryAddress { get; set; }

        [RuleRequiredField]
        [StringLength(500)]
        public virtual string CargoDescription { get; set; }

        [RuleValueComparison("CargoWeightKg_Positive", DefaultContexts.Save,
            ValueComparisonType.GreaterThan, 0)]
        public virtual decimal CargoWeightKg { get; set; }

        [RuleRequiredField]
        [RuleValueComparison("RequestedPickupDate_TodayOrLater", 
            DefaultContexts.Save, 
            ValueComparisonType.GreaterThanOrEqual, "[Now]")]
        public virtual DateTime RequestedPickupDate { get; set; }

        public virtual DateTime? ActualPickupDate { get; set; }

        public virtual DateTime? ActualDeliveryDate { get; set; }

        public virtual DeliveryOrderStatus Status { get; set; } = DeliveryOrderStatus.Created;

        public virtual Vehicle AssignedVehicle { get; set; }

        public virtual Driver AssignedDriver { get; set; }

        [StringLength(1000)]
        public virtual string Notes { get; set; }
    }
}
