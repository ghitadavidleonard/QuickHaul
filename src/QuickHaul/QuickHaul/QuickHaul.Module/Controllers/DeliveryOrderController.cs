using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using QuickHaul.Module.BusinessObjects;
using QuickHaul.Module.BusinessObjects.Enums;
using System.ComponentModel;

namespace QuickHaul.Module.Controllers
{
    public class DeliveryOrderController : ObjectViewController<DetailView, DeliveryOrder>
    {
        private readonly PopupWindowShowAction _assignAndDispatchAction;
        private readonly PopupWindowShowAction _confirmPickupAction;
        private readonly PopupWindowShowAction _confirmDeliveryAction;
        private readonly PopupWindowShowAction _closeOrderAction;
        private readonly PopupWindowShowAction _cancelAction;

        // XAF requires actions to be created in the constructor so they are
        // registered with the framework before the view opens.
        public DeliveryOrderController()
        {
            _assignAndDispatchAction = new PopupWindowShowAction(this, "AssignAndDispatch", PredefinedCategory.Edit);
            _assignAndDispatchAction.Caption = "Assign && Dispatch";
            _assignAndDispatchAction.ToolTip = "Validate prerequisites and dispatch the order.";
            _assignAndDispatchAction.CustomizePopupWindowParams += Action_CustomizePopupWindowParams;
            _assignAndDispatchAction.Execute += AssignAndDispatch_Execute;

            _confirmPickupAction = new PopupWindowShowAction(this, "ConfirmPickup", PredefinedCategory.Edit);
            _confirmPickupAction.Caption = "Confirm Pickup";
            _confirmPickupAction.ToolTip = "Record that the cargo has been picked up.";
            _confirmPickupAction.CustomizePopupWindowParams += Action_CustomizePopupWindowParams;
            _confirmPickupAction.Execute += ConfirmPickup_Execute;

            _confirmDeliveryAction = new PopupWindowShowAction(this, "ConfirmDelivery", PredefinedCategory.Edit);
            _confirmDeliveryAction.Caption = "Confirm Delivery";
            _confirmDeliveryAction.ToolTip = "Record that the cargo has been delivered.";
            _confirmDeliveryAction.CustomizePopupWindowParams += Action_CustomizePopupWindowParams;
            _confirmDeliveryAction.Execute += ConfirmDelivery_Execute;

            _closeOrderAction = new PopupWindowShowAction(this, "CloseOrder", PredefinedCategory.Edit);
            _closeOrderAction.Caption = "Close Order";
            _closeOrderAction.ToolTip = "Finalize the order and release the vehicle.";
            _closeOrderAction.CustomizePopupWindowParams += Action_CustomizePopupWindowParams;
            _closeOrderAction.Execute += CloseOrder_Execute;

            _cancelAction = new PopupWindowShowAction(this, "CancelOrder", PredefinedCategory.Edit);
            _cancelAction.Caption = "Cancel Order";
            _cancelAction.ToolTip = "Cancel this delivery order.";
            _cancelAction.CustomizePopupWindowParams += Action_CustomizePopupWindowParams;
            _cancelAction.Execute += Cancel_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            // Watch for Status changes triggered by data refreshes so button
            // visibility stays in sync automatically.
            ((INotifyPropertyChanged)ViewCurrentObject).PropertyChanged += Order_PropertyChanged;

            if (ObjectSpace.IsNewObject(ViewCurrentObject))
                ObjectSpace.Committing += ObjectSpace_Committing;

            UpdateActionStates();
        }

        private void Order_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeliveryOrder.Status))
                UpdateActionStates();
        }

        // Controls which buttons are visible by toggling the Active["ValidStatus"] flag.
        // action.Active is an AND-dictionary: the button is shown only when every
        // registered flag is true. Setting one flag to false hides the button.
        private void UpdateActionStates()
        {
            var status = ViewCurrentObject?.Status;

            _assignAndDispatchAction.Active["ValidStatus"] = status == DeliveryOrderStatus.Created;
            _confirmPickupAction.Active["ValidStatus"] = status == DeliveryOrderStatus.Dispatched;
            _confirmDeliveryAction.Active["ValidStatus"] = status == DeliveryOrderStatus.InTransit;
            _closeOrderAction.Active["ValidStatus"] = status == DeliveryOrderStatus.Delivered;
            _cancelAction.Active["ValidStatus"] =
                status == DeliveryOrderStatus.Created || status == DeliveryOrderStatus.Dispatched;
        }

        // Shared popup builder: creates a fresh TransitionRemarksParameters instance
        // and tells XAF to render it as the popup's form.
        private void Action_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            var objectSpace = Application.CreateObjectSpace(typeof(TransitionRemarksParameters));
            var parameters  = objectSpace.CreateObject<TransitionRemarksParameters>();
            e.View = Application.CreateDetailView(objectSpace, parameters);
        }

        // ── Transition: Created → Dispatched ────────────────────────────────────
        private void AssignAndDispatch_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var order = ViewCurrentObject;
            var remarks = ((TransitionRemarksParameters)e.PopupWindowViewCurrentObject).Remarks;

            if (order.AssignedVehicle == null)
                throw new UserFriendlyException("A vehicle must be assigned before dispatching.");
            if (order.AssignedDriver == null)
                throw new UserFriendlyException("A driver must be assigned before dispatching.");
            if (order.AssignedVehicle.Status != VehicleStatus.Available)
                throw new UserFriendlyException(
                    $"Vehicle '{order.AssignedVehicle.RegistrationPlate}' is not available " +
                    $"(current status: {order.AssignedVehicle.Status}).");
            if (!order.AssignedDriver.IsActive)
                throw new UserFriendlyException(
                    $"Driver '{order.AssignedDriver.FullName}' is not active.");

            var requiredLicense = GetRequiredLicense(order.AssignedVehicle.VehicleClass);
            if ((order.AssignedDriver.LicenseClasses & requiredLicense) != requiredLicense)
                throw new UserFriendlyException(
                    $"Driver '{order.AssignedDriver.FullName}' does not hold the required " +
                    $"{requiredLicense} license for this vehicle class.");

            if (order.CargoWeightKg > order.AssignedVehicle.PayloadCapacityKg)
                throw new UserFriendlyException(
                    $"Cargo weight ({order.CargoWeightKg} kg) exceeds the vehicle's " +
                    $"payload capacity ({order.AssignedVehicle.PayloadCapacityKg} kg).");

            // ObjectSpace.GetObject ensures we edit the vehicle through the same
            // Unit-of-Work so both changes are committed atomically.
            var vehicle = ObjectSpace.GetObject(order.AssignedVehicle);
            vehicle.Status = VehicleStatus.IsUse;

            CreateDeliveryEvent(order, order.Status, DeliveryOrderStatus.Dispatched, remarks);
            order.Status = DeliveryOrderStatus.Dispatched;

            ObjectSpace.CommitChanges();
            UpdateActionStates();
            View.Refresh();
        }

        // ── Transition: Dispatched → InTransit ──────────────────────────────────
        private void ConfirmPickup_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var order = ViewCurrentObject;
            var remarks = ((TransitionRemarksParameters)e.PopupWindowViewCurrentObject).Remarks;

            order.ActualPickupDate ??= DateTime.Now;

            CreateDeliveryEvent(order, order.Status, DeliveryOrderStatus.InTransit, remarks);
            order.Status = DeliveryOrderStatus.InTransit;

            ObjectSpace.CommitChanges();
            UpdateActionStates();
            View.Refresh();
        }

        // ── Transition: InTransit → Delivered ───────────────────────────────────
        private void ConfirmDelivery_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var order = ViewCurrentObject;
            var remarks = ((TransitionRemarksParameters)e.PopupWindowViewCurrentObject).Remarks;

            order.ActualDeliveryDate ??= DateTime.Now;

            CreateDeliveryEvent(order, order.Status, DeliveryOrderStatus.Delivered, remarks);
            order.Status = DeliveryOrderStatus.Delivered;

            ObjectSpace.CommitChanges();
            UpdateActionStates();
            View.Refresh();
        }

        // ── Transition: Delivered → Closed ──────────────────────────────────────
        private void CloseOrder_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var order = ViewCurrentObject;
            var remarks = ((TransitionRemarksParameters)e.PopupWindowViewCurrentObject).Remarks;

            if (order.AssignedVehicle != null)
            {
                var vehicle = ObjectSpace.GetObject(order.AssignedVehicle);
                vehicle.Status = VehicleStatus.Available;
            }

            CreateDeliveryEvent(order, order.Status, DeliveryOrderStatus.Closed, remarks);
            order.Status = DeliveryOrderStatus.Closed;

            ObjectSpace.CommitChanges();
            UpdateActionStates();
            View.Refresh();
        }

        // ── Transition: Created|Dispatched → Cancelled ──────────────────────────
        private void Cancel_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var order = ViewCurrentObject;
            var remarks = ((TransitionRemarksParameters)e.PopupWindowViewCurrentObject).Remarks;
            var fromStatus = order.Status; // capture before changing it

            // Release the vehicle whether we're cancelling from Created or Dispatched.
            if (order.AssignedVehicle != null)
            {
                var vehicle = ObjectSpace.GetObject(order.AssignedVehicle);
                vehicle.Status = VehicleStatus.Available;
            }

            CreateDeliveryEvent(order, fromStatus, DeliveryOrderStatus.Cancelled, remarks);
            order.Status = DeliveryOrderStatus.Cancelled;

            ObjectSpace.CommitChanges();
            UpdateActionStates();
            View.Refresh();
        }

        // ── Shared helpers ───────────────────────────────────────────────────────

        private void CreateDeliveryEvent(DeliveryOrder order, DeliveryOrderStatus? fromStatus,
            DeliveryOrderStatus toStatus, string remarks)
        {
            var evt = ObjectSpace.CreateObject<DeliveryEvent>();
            evt.DeliveryOrder = order;
            evt.Timestamp = DateTime.UtcNow;
            evt.FromStatus = fromStatus;
            evt.ToStatus = toStatus;
            evt.ChangedBy = GetCurrentUserName();
            evt.Remarks = remarks;
        }

        // Fires only on the very first save of a new order.
        // Generates the sequential OrderNumber and the initial "Created" audit event.
        private void ObjectSpace_Committing(object sender, EventArgs e)
        {
            ObjectSpace.Committing -= ObjectSpace_Committing;

            var order = ViewCurrentObject;
            if (string.IsNullOrEmpty(order.OrderNumber))
            {
                string dateKey = DateTime.Today.ToString("yyyyMMdd");
                var sequence   = ObjectSpace.FirstOrDefault<OrderSequence>(s => s.DateKey == dateKey);
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

        // Maps VehicleClass → the corresponding LicenseClasses flag.
        // A direct cast is unsafe because VehicleClass.HeavyTruck = 3
        // while LicenseClasses.HeavyTruck = 4 (Flags bit).
        private static LicenseClasses GetRequiredLicense(VehicleClass vehicleClass) => vehicleClass switch
        {
            VehicleClass.Van => LicenseClasses.Van,
            VehicleClass.Truck => LicenseClasses.Truck,
            VehicleClass.HeavyTruck => LicenseClasses.HeavyTruck,
            _ => throw new InvalidOperationException($"Unknown vehicle class: {vehicleClass}")
        };

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
