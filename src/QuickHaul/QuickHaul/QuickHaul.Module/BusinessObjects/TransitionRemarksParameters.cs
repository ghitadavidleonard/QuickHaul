using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    [DomainComponent]
    public class TransitionRemarksParameters
    {
        [Browsable(false)]
        [Key]
        public Guid Oid { get; set; } = Guid.NewGuid();

        [StringLength(500)]
        [ModelDefault("RowCount", "3")]
        public string Remarks { get; set; }
    }
}
