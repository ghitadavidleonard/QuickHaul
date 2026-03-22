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
                dispatcherRole.AddTypePermission<Vehicle>(SecurityOperations.ReadOnlyAccess, SecurityPermissionState.Allow);
                dispatcherRole.AddTypePermission<Driver>(SecurityOperations.ReadOnlyAccess, SecurityPermissionState.Allow);
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
            }

            return fleetManagerRole;
        }
    }
}
