using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class Customer: BaseObject
    {
        [RuleRequiredField]
        [StringLength(150)]
        public virtual string CompanyName { get; set; }

        [RuleRequiredField]
        [StringLength(100)]
        public virtual string ContactPerson { get; set; }

        [RuleRequiredField]
        [StringLength(150)]
        [RuleRegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$")]
        public virtual string Email{ get; set; }

        [RuleRequiredField]
        [StringLength(20)]
        [RuleRegularExpression(@"^\+?[0-9]{7,15}$")]
        public virtual string Phone { get; set; }

        [RuleRequiredField]
        [StringLength(300)]
        public virtual string BillingAddress { get; set; }
    }
}
