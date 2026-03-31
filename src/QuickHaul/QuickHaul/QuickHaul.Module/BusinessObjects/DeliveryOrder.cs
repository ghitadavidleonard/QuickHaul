using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using QuickHaul.Module.BusinessObjects.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        [ModelDefault("DisplayFormat", "{0:N2}")]
        [ModelDefault("EditMask", "n2")]
        public virtual decimal CargoWeightKg { get; set; }

        [RuleRequiredField]
        public virtual DateTime RequestedPickupDate { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual DateTime? ActualPickupDate { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual DateTime? ActualDeliveryDate { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual DeliveryOrderStatus Status { get; set; } = DeliveryOrderStatus.Created;

        public virtual Vehicle AssignedVehicle { get; set; }
        public virtual Driver AssignedDriver { get; set; }

        [StringLength(1000)]
        public virtual string Notes { get; set; }

        [ModelDefault("AllowEdit", "False")]
        [ModelDefault("AllowNew", "False")]
        [ModelDefault("AllowDelete", "False")]
        public virtual ObservableCollection<DeliveryEvent> DeliveryEvents { get; set; } = new ObservableCollection<DeliveryEvent>();

        [RuleFromBoolProperty("CargoWeightKg_VehicleCapacity", DefaultContexts.Save,
            "Cargo weight exceeds the assigned vehicle's payload capacity.",
            SkipNullOrEmptyValues = false,
            UsedProperties = "CargoWeightKg, AssignedVehicle")]
        [Browsable(false)]
        public bool CargoWeightWithinVehicleCapacity =>
            AssignedVehicle == null || CargoWeightKg <= AssignedVehicle.PayloadCapacityKg;

        [RuleFromBoolProperty(
            "AssignedDriver_MustBeActive",
            DefaultContexts.Save,
            "Only active drivers can be assigned to a delivery order.",
            SkipNullOrEmptyValues = false,
            UsedProperties = "AssignedDriver")]
        [Browsable(false)]
        public bool AssignedDriverMustBeActive =>
            AssignedDriver == null || AssignedDriver.IsActive;
    }
}
