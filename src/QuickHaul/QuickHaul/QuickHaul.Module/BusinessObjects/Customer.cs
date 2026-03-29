using DevExpress.ExpressApp.Model;
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
        [RuleRegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", CustomMessageTemplate = "Invalid email address. Please use the format name@domain.com.")]
        public virtual string Email{ get; set; }

        [RuleRequiredField]
        [StringLength(20)]
        [RuleRegularExpression(@"^\+?[0-9]{7,15}$", CustomMessageTemplate = "Please enter a valid phone number (7–15 digits), optionally starting with '+', e.g. +15551234567.")]
        public virtual string Phone { get; set; }

        [RuleRequiredField]
        [StringLength(300)]
        [ModelDefault("RowCount", "3")]
        public virtual string BillingAddress { get; set; }
    }
}
