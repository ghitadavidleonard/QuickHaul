using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using QuickHaul.Module.BusinessObjects.Enums;
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
        public virtual decimal PayloadCapacityKg { get; set; }

        public virtual VehicleStatus Status { get; set; } = VehicleStatus.Available;

        [StringLength(200)]
        public virtual string CurrentLocation { get; set; }

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
