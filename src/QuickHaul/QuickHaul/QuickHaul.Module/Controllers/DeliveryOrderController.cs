using DevExpress.ExpressApp;
using QuickHaul.Module.BusinessObjects;

namespace QuickHaul.Module.Controllers
{
    public class DeliveryOrderController : ObjectViewController<DetailView, DeliveryOrder>
    {
        protected override void OnActivated()
        {
            base.OnActivated();
            if (ObjectSpace.IsNewObject(ViewCurrentObject))
                ObjectSpace.Committing += ObjectSpace_Committing;
        }

        private void ObjectSpace_Committing(object sender, EventArgs e)
        {
            var order = ViewCurrentObject;
            if (!string.IsNullOrEmpty(order.OrderNumber))
                return;

            string dateKey = DateTime.Today.ToString("yyyyMMdd");

            var sequence = ObjectSpace.FirstOrDefault<OrderSequence>(s => s.DateKey == dateKey);
            if (sequence == null)
            {
                sequence = ObjectSpace.CreateObject<OrderSequence>();
                sequence.DateKey = dateKey;
                sequence.LastSequence = 0;
            }

            sequence.LastSequence++;
            order.OrderNumber = $"DLV-{dateKey}-{sequence.LastSequence:D4}";
        }

        protected override void OnDeactivated()
        {
            ObjectSpace.Committing -= ObjectSpace_Committing;
            base.OnDeactivated();
        }
    }
}
