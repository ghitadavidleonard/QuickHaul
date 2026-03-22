using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using QuickHaul.Module.BusinessObjects.Enums;
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
        [RuleRegularExpression(@"^\+?[0-9]{7,15}$")]
        public virtual string PhoneNumber { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [RuleRequiredField]
        [RuleCriteria(DefaultContexts.Save, "HireDate <= LocalDateTimeNow()", CustomMessageTemplate = "Hire Date cannot be in the future.")]
        public virtual DateTime HireDate { get; set; }
    }
}
