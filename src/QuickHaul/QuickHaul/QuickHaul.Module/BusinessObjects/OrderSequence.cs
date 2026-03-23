using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    [Browsable(false)]
    public class OrderSequence : BaseObject
    {
        [StringLength(8)]
        public virtual string DateKey { get; set; }

        public virtual int LastSequence { get; set; }
    }
}
