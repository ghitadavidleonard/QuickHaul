using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using System.ComponentModel.DataAnnotations;

namespace QuickHaul.Module.BusinessObjects
{
    [DomainComponent]
    public class TransitionRemarksParameters
    {
        // XAF requires a key property on non-persistent domain components.
        // It is hidden from the UI automatically.
        [Key]
        public Guid Oid { get; set; } = Guid.NewGuid();

        [StringLength(500)]
        [ModelDefault("RowCount", "3")]
        public string Remarks { get; set; }
    }
}
