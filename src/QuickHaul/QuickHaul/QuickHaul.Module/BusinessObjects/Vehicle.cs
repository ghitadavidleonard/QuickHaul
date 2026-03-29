using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using QuickHaul.Module.BusinessObjects.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class Vehicle : BaseObject
    {
        [RuleRequiredField]
        [RuleUniqueValue]
        [StringLength(20)]
        public virtual string RegistrationPlate { get; set; }

        [RuleRequiredField]
        [StringLength(50)]
        public virtual string Make { get; set; }

        [RuleRequiredField]
        [StringLength(50)]
        public virtual string Model { get; set; }

        public virtual VehicleClass VehicleClass { get; set; }

        [RuleValueComparison(ValueComparisonType.GreaterThan, 0)]
        [ModelDefault("DisplayFormat", "{0:N2}")]
        [ModelDefault("EditMask", "n2")]
        public virtual decimal PayloadCapacityKg { get; set; }

        public virtual VehicleStatus Status { get; set; } = VehicleStatus.Available;

        [StringLength(200)]
        public virtual string CurrentLocation { get; set; }

        [RuleFromBoolProperty(
            "Vehicle_CannotDelete_WhenReferencedByActiveOrders",
            DefaultContexts.Delete,
            "Cannot delete this vehicle because it is referenced by an active delivery order.")]
        [Browsable(false)]
        public bool CanDeleteWhenNotReferencedByActiveOrders
        {
            get
            {
                if (ObjectSpace == null)
                    return true;

                return !ObjectSpace.GetObjectsQuery<DeliveryOrder>()
                    .Any(o =>
                        o.AssignedVehicle != null &&
                        o.AssignedVehicle.ID == ID &&
                        o.Status != DeliveryOrderStatus.Closed &&
                        o.Status != DeliveryOrderStatus.Cancelled);
            }
        }

        public override void OnSaving()
        {
            base.OnSaving();

            if(!string.IsNullOrWhiteSpace(RegistrationPlate))
            {
                RegistrationPlate = RegistrationPlate.ToUpper().Replace(" ", "");
            }
        }
    }
}
