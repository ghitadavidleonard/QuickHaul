using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using QuickHaul.Module.BusinessObjects.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class Driver: BaseObject
    {
        [RuleRequiredField]
        [StringLength(100)]
        public virtual string FullName { get; set; }

        [RuleRequiredField]
        [RuleUniqueValue]
        [StringLength(30)]
        public virtual string LicenseNumber { get; set; }

        public virtual LicenseClasses LicenseClasses { get; set; }

        [RuleRequiredField]
        [StringLength(20)]
        [RuleRegularExpression(@"^\+?[0-9]{7,15}$", CustomMessageTemplate="The phone number must be a valid one!")]
        public virtual string PhoneNumber { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [RuleRequiredField]
        public virtual DateTime HireDate { get; set; }

        [RuleFromBoolProperty("HireDate_NotInFuture", DefaultContexts.Save,
            "Hire date cannot be in the future.",
            SkipNullOrEmptyValues = false,
            UsedProperties = "HireDate")]
        [Browsable(false)]
        public bool HireDateNotInFuture => HireDate <= DateTime.Now;
    }
}
