using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
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

        private void ShowError(string message)
        {
            Application.ShowViewStrategy.ShowMessage(new MessageOptions
            {
                Message = message,
                Type = InformationType.Error,
                Duration = 4000
            });
        }

        private void UpdateActionStates()
        {
            var status = ViewCurrentObject?.Status;
            bool isFleetManager =
                (SecuritySystem.CurrentUser as PermissionPolicyUser)?.Roles.Any(r => r.Name == "FleetManager") == true;

            _assignAndDispatchAction.Active["Role"] = !isFleetManager;
            _confirmPickupAction.Active["Role"] = !isFleetManager;
            _confirmDeliveryAction.Active["Role"] = !isFleetManager;
            _closeOrderAction.Active["Role"] = !isFleetManager;
            _cancelAction.Active["Role"] = !isFleetManager;

            _assignAndDispatchAction.Active["ValidStatus"] = status == DeliveryOrderStatus.Created;
            _confirmPickupAction.Active["ValidStatus"] = status == DeliveryOrderStatus.Dispatched;
            _confirmDeliveryAction.Active["ValidStatus"] = status == DeliveryOrderStatus.InTransit;
            _closeOrderAction.Active["ValidStatus"] = status == DeliveryOrderStatus.Delivered;
            _cancelAction.Active["ValidStatus"] =
                status == DeliveryOrderStatus.Created || status == DeliveryOrderStatus.Dispatched;
        }

        private void Action_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            var objectSpace = Application.CreateObjectSpace(typeof(TransitionRemarksParameters));
            var parameters  = objectSpace.CreateObject<TransitionRemarksParameters>();
            e.View = Application.CreateDetailView(objectSpace, parameters);
        }

        private void AssignAndDispatch_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var order = ViewCurrentObject;
            var remarks = ((TransitionRemarksParameters)e.PopupWindowViewCurrentObject).Remarks;

            if(ObjectSpace.IsObjectToSave(order))
            {
                ShowError("Please save the order before dispatching.");
                return;
            }

            if (order.AssignedVehicle == null)
            {
                ShowError("A vehicle must be assigned before dispatching.");
                return;
            }

            if (order.AssignedDriver == null)
            {
                ShowError("A driver must be assigned before dispatching.");
                return;
            }

            if (order.AssignedVehicle?.Status != VehicleStatus.Available)
            {
                ShowError($"Vehicle '{order.AssignedVehicle.RegistrationPlate}' is not available " +
                    $"(current status: {order.AssignedVehicle.Status}).");
                return;
            }

            if (!order.AssignedDriver.IsActive)
            {
                ShowError($"Driver '{order.AssignedDriver.FullName}' is not active.");
                return;
            }

            var driver = ObjectSpace.GetObject(order.AssignedDriver);
            var driverHasAnotherActiveOrder = ObjectSpace
                .GetObjectsQuery<DeliveryOrder>()
                .Any(o =>
                    o.ID != order.ID &&
                    o.AssignedDriver != null &&
                    o.AssignedDriver.ID == driver.ID &&
                    (o.Status == DeliveryOrderStatus.Dispatched || o.Status == DeliveryOrderStatus.InTransit));

            if (driverHasAnotherActiveOrder)
            {
                ShowError($"Driver '{order.AssignedDriver.FullName}' is already assigned to an active delivery.");
                return;
            }

            var requiredLicense = GetRequiredLicense(order.AssignedVehicle.VehicleClass);
            if ((order.AssignedDriver.LicenseClasses & requiredLicense) != requiredLicense)
            {
                ShowError($"Driver '{order.AssignedDriver.FullName}' does not hold the required " +
                    $"{requiredLicense} license for this vehicle class.");
                return;
            }

            if (order.CargoWeightKg > order.AssignedVehicle.PayloadCapacityKg)
            {
                ShowError(
                    $"Cargo weight ({order.CargoWeightKg} kg) exceeds the vehicle's " +
                    $"payload capacity ({order.AssignedVehicle.PayloadCapacityKg} kg).");
                return;
            }

            var vehicle = order.AssignedVehicle;
            vehicle.Status = VehicleStatus.IsUse;

            CreateDeliveryEvent(order, order.Status, DeliveryOrderStatus.Dispatched, remarks);
            order.Status = DeliveryOrderStatus.Dispatched;

            ObjectSpace.CommitChanges();
            UpdateActionStates();
            View.Refresh();
        }

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

        private void Cancel_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var order = ViewCurrentObject;
            var remarks = ((TransitionRemarksParameters)e.PopupWindowViewCurrentObject).Remarks;

            // Release the vehicle whether we're cancelling from Created or Dispatched.
            if (order.AssignedVehicle != null)
            {
                var vehicle = ObjectSpace.GetObject(order.AssignedVehicle);
                vehicle.Status = VehicleStatus.Available;
            }

            CreateDeliveryEvent(order, order.Status, DeliveryOrderStatus.Cancelled, remarks);
            order.Status = DeliveryOrderStatus.Cancelled;

            ObjectSpace.CommitChanges();
            UpdateActionStates();
            View.Refresh();
        }

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

        private void ObjectSpace_Committing(object sender, EventArgs e)
        {
            ObjectSpace.Committing -= ObjectSpace_Committing;

            var order = ViewCurrentObject;
            if (string.IsNullOrEmpty(order.OrderNumber))
            {
                if (order.RequestedPickupDate < DateTime.Today)
                {
                    ShowError("Requested pickup date must be today or later.");
                    return;
                }

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
