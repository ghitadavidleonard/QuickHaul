using DevExpress.ExpressApp;
using QuickHaul.Module.BusinessObjects;
using QuickHaul.Module.BusinessObjects.Enums;
using System.ComponentModel;

namespace QuickHaul.Module.Controllers
{
    public class DeliveryOrderController : ObjectViewController<DetailView, DeliveryOrder>
    {
        protected override void OnActivated()
        {
            base.OnActivated();
            ((INotifyPropertyChanged)ViewCurrentObject).PropertyChanged += Order_PropertyChanged;
            if (ObjectSpace.IsNewObject(ViewCurrentObject))
                ObjectSpace.Committing += ObjectSpace_Committing;
        }

        private void Order_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DeliveryOrder.Status)) return;
            var order = (DeliveryOrder)sender;
            if (order.Status == DeliveryOrderStatus.InTransit && order.ActualPickupDate == null)
                order.ActualPickupDate = DateTime.Now;
            else if (order.Status == DeliveryOrderStatus.Delivered && order.ActualDeliveryDate == null)
                order.ActualDeliveryDate = DateTime.Now;
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
            ((INotifyPropertyChanged)ViewCurrentObject).PropertyChanged -= Order_PropertyChanged;
            ObjectSpace.Committing -= ObjectSpace_Committing;
            base.OnDeactivated();
        }
    }
}
