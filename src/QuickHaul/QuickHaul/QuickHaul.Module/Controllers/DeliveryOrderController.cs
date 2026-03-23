using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using QuickHaul.Module.BusinessObjects;
using QuickHaul.Module.BusinessObjects.Enums;
using System.ComponentModel;

namespace QuickHaul.Module.Controllers
{
    public class DeliveryOrderController : ObjectViewController<DetailView, DeliveryOrder>
    {
        private DeliveryOrderStatus _previousStatus;

        protected override void OnActivated()
        {
            base.OnActivated();
            _previousStatus = ViewCurrentObject.Status;
            ((INotifyPropertyChanged)ViewCurrentObject).PropertyChanged += Order_PropertyChanged;
            if (ObjectSpace.IsNewObject(ViewCurrentObject))
                ObjectSpace.Committing += ObjectSpace_Committing;
        }

        private void Order_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DeliveryOrder.Status)) return;
            var order = (DeliveryOrder)sender;

            if (order.Status == DeliveryOrderStatus.InTransit && order.ActualPickupDate == null)
                order.ActualPickupDate = DateTime.Now;
            else if (order.Status == DeliveryOrderStatus.Delivered && order.ActualDeliveryDate == null)
                order.ActualDeliveryDate = DateTime.Now;

            var evt = ObjectSpace.CreateObject<DeliveryEvent>();
            evt.DeliveryOrder = order;
            evt.Timestamp = DateTime.UtcNow;
            evt.FromStatus = _previousStatus;
            evt.ToStatus = order.Status;
            evt.ChangedBy = GetCurrentUserName();

            _previousStatus = order.Status;
        }

        private void ObjectSpace_Committing(object sender, EventArgs e)
        {
            ObjectSpace.Committing -= ObjectSpace_Committing;

            var order = ViewCurrentObject;
            if (string.IsNullOrEmpty(order.OrderNumber))
            {
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

            var creationEvent = ObjectSpace.CreateObject<DeliveryEvent>();
            creationEvent.DeliveryOrder = order;
            creationEvent.Timestamp = DateTime.UtcNow;
            creationEvent.FromStatus = null;
            creationEvent.ToStatus = DeliveryOrderStatus.Created;
            creationEvent.ChangedBy = GetCurrentUserName();
        }

        private string GetCurrentUserName() =>
            (SecuritySystem.CurrentUser as PermissionPolicyUser)?.UserName ?? "System";

        protected override void OnDeactivated()
        {
            ((INotifyPropertyChanged)ViewCurrentObject).PropertyChanged -= Order_PropertyChanged;
            ObjectSpace.Committing -= ObjectSpace_Committing;
            base.OnDeactivated();
        }
    }
}
