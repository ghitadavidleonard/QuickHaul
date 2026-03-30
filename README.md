# QuickHaul

A delivery and hauling management platform built with **DevExpress XAF** (eXpressApp Framework) on **Blazor Server** and **.NET 8**.

QuickHaul lets dispatchers manage customers, drivers, vehicles, and delivery orders through an auto-generated UI with role-based security, validation, dashboards, and a built-in Web API.

---

## Prerequisites

| Tool                                                                           | Version                           |
| ------------------------------------------------------------------------------ | --------------------------------- |
| [.NET SDK](https://dotnet.microsoft.com/download)                              | 8.0 or later                      |
| [SQL Server](https://www.microsoft.com/sql-server)                             | 2019+ (LocalDB, Express, or full) |
| [DevExpress NuGet Feed](https://docs.devexpress.com/GeneralInformation/116042) | 25.2.x (requires a license)       |

> **DevExpress packages** are restored from the DevExpress NuGet feed. Add it once with:
>
> ```shell
> dotnet nuget add source https://nuget.devexpress.com/api -n DevExpress -u <your-email> -p <your-feed-key> --store-password-in-clear-text
> ```

---

## Getting Started

### 1. Clone the repository

```shell
git clone https://github.com/ghitadavidleonard/QuickHaul.git
cd QuickHaul/src/QuickHaul
```

### 2. Configure User Secrets

The application stores sensitive settings in [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets).

```shell
cd QuickHaul.Blazor.Server

dotnet user-secrets set "ConnectionStrings:ConnectionString" "Data Source=<YOUR_SQL_SERVER>;Initial Catalog=QuickHaul;Integrated Security=SSPI;TrustServerCertificate=True;Multiple Active Result Sets=True"
dotnet user-secrets set "Authentication:IssuerSigningKey" "<RANDOM-GUID-OR-SECRET>"
dotnet user-secrets set "DevExpress:ExpressApp:Security:UrlSigningKey" "<RANDOM-GUID-OR-SECRET>"
```

Replace `<YOUR_SQL_SERVER>` with your SQL Server instance (e.g. `(localdb)\MSSQLLocalDB` or `localhost`).

### 3. Restore packages and build

```shell
dotnet restore
dotnet build
```

### 4. Create / update the database

On first run XAF creates and seeds the database automatically. To do it explicitly:

```shell
dotnet run --project QuickHaul.Blazor.Server -- --updateDatabase --silent
```

### 5. Run the application

```shell
dotnet run --project QuickHaul.Blazor.Server
```

Open the URL shown in the console (e.g. `https://localhost:5001`). Log in with the default admin account created by the XAF module updater.

---

## License

This project uses DevExpress components that require a [DevExpress license](https://www.devexpress.com/buy/).
