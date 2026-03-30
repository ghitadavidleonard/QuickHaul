using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EF;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.Extensions.DependencyInjection;
using QuickHaul.Module.BusinessObjects;
using QuickHaul.Module.BusinessObjects.Enums;

namespace QuickHaul.Module.DatabaseUpdate
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }
        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            //string name = "MyName";
            //EntityObject1 theObject = ObjectSpace.FirstOrDefault<EntityObject1>(u => u.Name == name);
            //if(theObject == null) {
            //    theObject = ObjectSpace.CreateObject<EntityObject1>();
            //    theObject.Name = name;
            //}

            // The code below creates users and roles for testing purposes only.
            // In production code, you can create users and assign roles to them automatically, as described in the following help topic:
            // https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication
#if !RELEASE
            // If a role doesn't exist in the database, create this role
            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();

            var dispatcherRole = CreateDispatcherRole();
            var fleetManager = CreateFleetManagerRole();

            ObjectSpace.CommitChanges(); //This line persists created object(s).

            UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

            // If a user named 'User' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) =>
                {
                    // Add the Users role to the user
                    user.Roles.Add(defaultRole);
                });
            }

            // If a user named 'Admin' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "Admin") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "Admin", EmptyPassword, (user) =>
                {
                    // Add the Administrators role to the user
                    user.Roles.Add(adminRole);
                });
            }

            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "dispatcher@quickhaul.local") == null)
            {
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "dispatcher@quickhaul.local", "Test123!", (user) =>
                {
                    user.Roles.Add(dispatcherRole);
                });
            }

            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "fleet@quickhaul.local") == null)
            {
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "fleet@quickhaul.local", "Test123!", (user) =>
                {
                    user.Roles.Add(fleetManager);
                });
            }

            ObjectSpace.CommitChanges(); //This line persists created object(s).

            SeedDemoData();
#endif
        }
        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();
        }
        PermissionPolicyRole CreateAdminRole()
        {
            PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
            if (adminRole == null)
            {
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
                adminRole.IsAdministrative = true;
            }
            return adminRole;
        }
        PermissionPolicyRole CreateDefaultRole()
        {
            PermissionPolicyRole defaultRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(role => role.Name == "Default");
            if (defaultRole == null)
            {
                defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                defaultRole.Name = "Default";

                defaultRole.AddObjectPermissionFromLambda<ApplicationUser>(SecurityOperations.Read, cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "ChangePasswordOnFirstLogon", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "StoredPassword", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddObjectPermission<ModelDifference>(SecurityOperations.ReadWriteAccess, "UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddObjectPermission<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, "Owner.UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
            }
            return defaultRole;
        }

        PermissionPolicyRole CreateDispatcherRole()
        {
            var dispatcherRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Dispatcher");
            if (dispatcherRole == null)
            {
                dispatcherRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                dispatcherRole.Name = "Dispatcher";
                dispatcherRole.AddTypePermission<DeliveryOrder>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                dispatcherRole.AddTypePermission<Customer>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                dispatcherRole.AddTypePermission<OrderSequence>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                dispatcherRole.AddTypePermission<DeliveryEvent>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                dispatcherRole.AddTypePermission<Vehicle>(SecurityOperations.ReadOnlyAccess, SecurityPermissionState.Allow);
                dispatcherRole.AddTypePermission<Vehicle>(SecurityOperations.Create, SecurityPermissionState.Deny);
                dispatcherRole.AddTypePermission<Vehicle>(SecurityOperations.Delete, SecurityPermissionState.Deny);
                dispatcherRole.AddTypePermission<Vehicle>(SecurityOperations.Write, SecurityPermissionState.Deny);
                dispatcherRole.AddTypePermission<Driver>(SecurityOperations.ReadOnlyAccess, SecurityPermissionState.Allow);
                dispatcherRole.AddTypePermission<Driver>(SecurityOperations.Create, SecurityPermissionState.Deny);
                dispatcherRole.AddTypePermission<Driver>(SecurityOperations.Delete, SecurityPermissionState.Deny);
                dispatcherRole.AddTypePermission<Driver>(SecurityOperations.Write, SecurityPermissionState.Deny);

                AddNavigationPermissions(dispatcherRole);
        }

            return dispatcherRole;
        }

        PermissionPolicyRole CreateFleetManagerRole()
        {
            var fleetManagerRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "FleetManager");
            if (fleetManagerRole == null)
            {
                fleetManagerRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                fleetManagerRole.Name = "FleetManager";
                fleetManagerRole.AddTypePermission<Vehicle>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                fleetManagerRole.AddTypePermission<Driver>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                fleetManagerRole.AddTypePermission<DeliveryOrder>(SecurityOperations.ReadOnlyAccess, SecurityPermissionState.Allow);
                fleetManagerRole.AddTypePermission<Customer>(SecurityOperations.ReadOnlyAccess, SecurityPermissionState.Allow);
                fleetManagerRole.AddTypePermission<DeliveryEvent>(SecurityOperations.ReadOnlyAccess, SecurityPermissionState.Allow);

                AddNavigationPermissions(fleetManagerRole);
            }

            return fleetManagerRole;
        }

        private void AddNavigationPermissions(PermissionPolicyRole role)
        {
            role.AddNavigationPermission(@"Application/NavigationItems/Items/OperationsNavigation/Items/DeliveryOrder_ListView", SecurityPermissionState.Allow);
            role.AddNavigationPermission(@"Application/NavigationItems/Items/ClientsNavigation/Items/Customer_ListView", SecurityPermissionState.Allow);
            role.AddNavigationPermission(@"Application/NavigationItems/Items/FleetNavigation/Items/Vehicle_ListView", SecurityPermissionState.Allow);
            role.AddNavigationPermission(@"Application/NavigationItems/Items/FleetNavigation/Items/Driver_ListView", SecurityPermissionState.Allow);
            role.AddNavigationPermission(@"Application/NavigationItems/Items/Reports/Items/Dashboards", SecurityPermissionState.Allow);
        }

        private void SeedDemoData()
        {
            if (ObjectSpace.FirstOrDefault<Vehicle>(_ => true) != null)
                return;

            // ── Vehicles ─────────────────────────────────────────────────────────

            var vFordTransit = ObjectSpace.CreateObject<Vehicle>();
            vFordTransit.RegistrationPlate = "QH001B";
            vFordTransit.Make = "Ford";
            vFordTransit.Model = "Transit";
            vFordTransit.VehicleClass = VehicleClass.Van;
            vFordTransit.PayloadCapacityKg = 1200m;
            vFordTransit.Status = VehicleStatus.Available;
            vFordTransit.CurrentLocation = "Bucharest Depot";

            var vSprinter = ObjectSpace.CreateObject<Vehicle>();
            vSprinter.RegistrationPlate = "QH002B";
            vSprinter.Make = "Mercedes-Benz";
            vSprinter.Model = "Sprinter 316";
            vSprinter.VehicleClass = VehicleClass.Van;
            vSprinter.PayloadCapacityKg = 1_500m;
            vSprinter.Status = VehicleStatus.IsUse;       // Dispatched order o2
            vSprinter.CurrentLocation = "Cluj-Napoca Hub";

            var vManTgm = ObjectSpace.CreateObject<Vehicle>();
            vManTgm.RegistrationPlate = "QH003B";
            vManTgm.Make = "MAN";
            vManTgm.Model = "TGM 18.290";
            vManTgm.VehicleClass = VehicleClass.Truck;
            vManTgm.PayloadCapacityKg = 7_000m;
            vManTgm.Status = VehicleStatus.IsUse;          // InTransit order o3
            vManTgm.CurrentLocation = "Timisoara Depot";

            var vVolvoFh = ObjectSpace.CreateObject<Vehicle>();
            vVolvoFh.RegistrationPlate = "QH004B";
            vVolvoFh.Make = "Volvo";
            vVolvoFh.Model = "FH 460";
            vVolvoFh.VehicleClass = VehicleClass.HeavyTruck;
            vVolvoFh.PayloadCapacityKg = 24_000m;
            vVolvoFh.Status = VehicleStatus.IsUse;          // Delivered order o4 — not yet Closed
            vVolvoFh.CurrentLocation = "Constanta Port";

            var vScania = ObjectSpace.CreateObject<Vehicle>();
            vScania.RegistrationPlate = "QH005B";
            vScania.Make = "Scania";
            vScania.Model = "R 450";
            vScania.VehicleClass = VehicleClass.HeavyTruck;
            vScania.PayloadCapacityKg = 22_000m;
            vScania.Status = VehicleStatus.Maintenance;
            vScania.CurrentLocation = "Bucharest Depot";

            // ── Drivers ──────────────────────────────────────────────────────────

            var dIon = ObjectSpace.CreateObject<Driver>();
            dIon.FullName = "Ion Popescu";
            dIon.LicenseNumber = "DL-RO-001";
            dIon.LicenseClasses = LicenseClasses.Van | LicenseClasses.Truck | LicenseClasses.HeavyTruck;
            dIon.PhoneNumber = "+40721000001";
            dIon.IsActive = true;
            dIon.HireDate = new DateTime(2019, 3, 15);

            var dMaria = ObjectSpace.CreateObject<Driver>();
            dMaria.FullName = "Maria Ionescu";
            dMaria.LicenseNumber = "DL-RO-002";
            dMaria.LicenseClasses = LicenseClasses.Van;
            dMaria.PhoneNumber = "+40721000002";
            dMaria.IsActive = true;
            dMaria.HireDate = new DateTime(2021, 6, 1);

            var dAndrei = ObjectSpace.CreateObject<Driver>();
            dAndrei.FullName = "Andrei Dumitrescu";
            dAndrei.LicenseNumber = "DL-RO-003";
            dAndrei.LicenseClasses = LicenseClasses.Van | LicenseClasses.Truck | LicenseClasses.HeavyTruck;
            dAndrei.PhoneNumber = "+40721000003";
            dAndrei.IsActive = true;
            dAndrei.HireDate = new DateTime(2018, 9, 10);

            // ── Customers ────────────────────────────────────────────────────────

            var cTechCorp = ObjectSpace.CreateObject<Customer>();
            cTechCorp.CompanyName = "TechCorp SRL";
            cTechCorp.ContactPerson = "Mihai Vasilescu";
            cTechCorp.Email = "office@techcorp.ro";
            cTechCorp.Phone = "+40311000001";
            cTechCorp.BillingAddress = "Str. Informaticii 12, Sector 1\nBucharest 010101\nRomania";

            var cFreshFoods = ObjectSpace.CreateObject<Customer>();
            cFreshFoods.CompanyName = "Fresh Foods SA";
            cFreshFoods.ContactPerson = "Elena Gheorghiu";
            cFreshFoods.Email = "logistics@freshfoods.ro";
            cFreshFoods.Phone = "+40311000002";
            cFreshFoods.BillingAddress = "Calea Florestilor 45\nCluj-Napoca 400000\nRomania";

            var cBuildRight = ObjectSpace.CreateObject<Customer>();
            cBuildRight.CompanyName = "BuildRight Construct SRL";
            cBuildRight.ContactPerson = "Calin Marin";
            cBuildRight.Email = "supply@buildright.ro";
            cBuildRight.Phone = "+40311000003";
            cBuildRight.BillingAddress = "Bd. Revolutiei 78\nTimisoara 300000\nRomania";

            // Persist base entities before creating orders (FK satisfaction)
            ObjectSpace.CommitChanges();

            // ── Delivery Orders ──────────────────────────────────────────────────
            // Status: Created — awaiting dispatch, no vehicle/driver yet

            var o1 = ObjectSpace.CreateObject<DeliveryOrder>();
            o1.OrderNumber = "QH-20260328-001";
            o1.Customer = cTechCorp;
            o1.PickupAddress = "Str. Informaticii 12, Sector 1, Bucharest";
            o1.DeliveryAddress = "Str. Industriilor 5, Brasov";
            o1.CargoDescription = "Server rack units and networking equipment - fragile";
            o1.CargoWeightKg = 480m;
            o1.RequestedPickupDate = new DateTime(2026, 3, 28);
            o1.Status = DeliveryOrderStatus.Created;
            o1.Notes = "Handle with care. Requires liftgate.";

            // Status: Dispatched — vehicle and driver assigned, awaiting pickup

            var o2 = ObjectSpace.CreateObject<DeliveryOrder>();
            o2.OrderNumber = "QH-20260320-002";
            o2.Customer = cFreshFoods;
            o2.PickupAddress = "Calea Florestilor 45, Cluj-Napoca";
            o2.DeliveryAddress = "Str. Principala 10, Oradea";
            o2.CargoDescription = "Chilled dairy products - temperature-sensitive";
            o2.CargoWeightKg = 820m;
            o2.RequestedPickupDate = new DateTime(2026, 3, 20);
            // ActualPickupDate intentionally null — pickup not yet confirmed
            o2.Status = DeliveryOrderStatus.Dispatched;
            o2.AssignedVehicle = vSprinter;
            o2.AssignedDriver = dMaria;
            o2.Notes = "Maintain cold chain. Deliver before 10:00.";

            // Status: InTransit — picked up and en route to destination

            var o3 = ObjectSpace.CreateObject<DeliveryOrder>();
            o3.OrderNumber = "QH-20260322-003";
            o3.Customer = cBuildRight;
            o3.PickupAddress = "Bd. Revolutiei 78, Timisoara";
            o3.DeliveryAddress = "Str. Santierului 3, Arad";
            o3.CargoDescription = "Steel beams, cement bags, and scaffolding parts";
            o3.CargoWeightKg = 6_800m;
            o3.RequestedPickupDate = new DateTime(2026, 3, 22);
            o3.ActualPickupDate = new DateTime(2026, 3, 22);   // Set during Dispatched → InTransit
            o3.Status = DeliveryOrderStatus.InTransit;
            o3.AssignedVehicle = vManTgm;
            o3.AssignedDriver = dAndrei;
            o3.Notes = "Delivery requires forklift on-site.";

            // Status: Delivered — cargo delivered, vehicle not yet released (still InUse)

            var o4 = ObjectSpace.CreateObject<DeliveryOrder>();
            o4.OrderNumber = "QH-20260210-004";
            o4.Customer = cTechCorp;
            o4.PickupAddress = "Str. Informaticii 12, Sector 1, Bucharest";
            o4.DeliveryAddress = "Parc Industrial Nord, Ploiesti";
            o4.CargoDescription = "UPS units and data centre cooling equipment";
            o4.CargoWeightKg = 15_200m;
            o4.RequestedPickupDate = new DateTime(2026, 2, 10);
            o4.ActualPickupDate = new DateTime(2026, 2, 10);   // Set during Dispatched → InTransit
            o4.ActualDeliveryDate = new DateTime(2026, 2, 11); // Set during InTransit → Delivered
            o4.Status = DeliveryOrderStatus.Delivered;
            o4.AssignedVehicle = vVolvoFh;
            o4.AssignedDriver = dIon;
            o4.Notes = "Delivery completed and signed off by client.";

            // Status: Cancelled — cancelled by customer before dispatch

            var o5 = ObjectSpace.CreateObject<DeliveryOrder>();
            o5.OrderNumber = "QH-20260225-005";
            o5.Customer = cFreshFoods;
            o5.PickupAddress = "Calea Florestilor 45, Cluj-Napoca";
            o5.DeliveryAddress = "Str. Marasesti 9, Sibiu";
            o5.CargoDescription = "Fresh seasonal vegetables - perishable";
            o5.CargoWeightKg = 190m;
            o5.RequestedPickupDate = new DateTime(2026, 2, 25);
            o5.Status = DeliveryOrderStatus.Cancelled;
            o5.Notes = "Cancelled by customer - order rescheduled.";

            ObjectSpace.CommitChanges();

            // ── Delivery Events (audit trail) ────────────────────────────────────
            // Each order must have events matching every transition it went through.

            const string seedUser = "system-seed";

            // o1: → Created
            CreateSeedEvent(o1, null, DeliveryOrderStatus.Created,
                new DateTime(2026, 3, 28, 8, 0, 0, DateTimeKind.Utc), seedUser);

            // o2: → Created → Dispatched
            CreateSeedEvent(o2, null, DeliveryOrderStatus.Created,
                new DateTime(2026, 3, 19, 14, 0, 0, DateTimeKind.Utc), seedUser);
            CreateSeedEvent(o2, DeliveryOrderStatus.Created, DeliveryOrderStatus.Dispatched,
                new DateTime(2026, 3, 20, 7, 0, 0, DateTimeKind.Utc), seedUser);

            // o3: → Created → Dispatched → InTransit
            CreateSeedEvent(o3, null, DeliveryOrderStatus.Created,
                new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc), seedUser);
            CreateSeedEvent(o3, DeliveryOrderStatus.Created, DeliveryOrderStatus.Dispatched,
                new DateTime(2026, 3, 22, 6, 0, 0, DateTimeKind.Utc), seedUser);
            CreateSeedEvent(o3, DeliveryOrderStatus.Dispatched, DeliveryOrderStatus.InTransit,
                new DateTime(2026, 3, 22, 9, 30, 0, DateTimeKind.Utc), seedUser);

            // o4: → Created → Dispatched → InTransit → Delivered
            CreateSeedEvent(o4, null, DeliveryOrderStatus.Created,
                new DateTime(2026, 2, 9, 11, 0, 0, DateTimeKind.Utc), seedUser);
            CreateSeedEvent(o4, DeliveryOrderStatus.Created, DeliveryOrderStatus.Dispatched,
                new DateTime(2026, 2, 10, 6, 0, 0, DateTimeKind.Utc), seedUser);
            CreateSeedEvent(o4, DeliveryOrderStatus.Dispatched, DeliveryOrderStatus.InTransit,
                new DateTime(2026, 2, 10, 8, 0, 0, DateTimeKind.Utc), seedUser);
            CreateSeedEvent(o4, DeliveryOrderStatus.InTransit, DeliveryOrderStatus.Delivered,
                new DateTime(2026, 2, 11, 15, 0, 0, DateTimeKind.Utc), seedUser);

            // o5: → Created → Cancelled
            CreateSeedEvent(o5, null, DeliveryOrderStatus.Created,
                new DateTime(2026, 2, 24, 9, 0, 0, DateTimeKind.Utc), seedUser);
            CreateSeedEvent(o5, DeliveryOrderStatus.Created, DeliveryOrderStatus.Cancelled,
                new DateTime(2026, 2, 25, 8, 0, 0, DateTimeKind.Utc), seedUser,
                "Cancelled by customer - order rescheduled.");

            ObjectSpace.CommitChanges();
        }

        private void CreateSeedEvent(
            DeliveryOrder order,
            DeliveryOrderStatus? fromStatus,
            DeliveryOrderStatus toStatus,
            DateTime timestampUtc,
            string changedBy,
            string remarks = null)
        {
            var evt = ObjectSpace.CreateObject<DeliveryEvent>();
            evt.DeliveryOrder = order;
            evt.Timestamp = timestampUtc;
            evt.FromStatus = fromStatus;
            evt.ToStatus = toStatus;
            evt.ChangedBy = changedBy;
            evt.Remarks = remarks;
        }
    }
}
