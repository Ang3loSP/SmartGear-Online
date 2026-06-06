# SmartGear Online 🛒

> A fully functional e-commerce web application built with **ASP.NET Core 8 MVC** & **C#**, developed as part of the Full Stack Web & Software Development programme at the Academic Institute of Excellence (AIE).

---

## 📌 Overview

SmartGear Online is a production-style e-commerce platform for customisable sports gear — covering the full development lifecycle from database design & user authentication through to real-time features, reporting & cloud deployment.

The platform supports product browsing, session-based cart management, order processing, product customisation, an admin dashboard, & real-time chat via SignalR.

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 MVC |
| Language | C# |
| Database | SQL Server (LocalDB for development) |
| ORM | Entity Framework Core 8 (code-first migrations) |
| Auth | ASP.NET Core Identity (roles: Admin, Customer) |
| Real-Time | SignalR (chat & inventory hubs) |
| Testing | xUnit + Moq |
| Caching | In-memory cache + response caching |
| Cloud | Microsoft Azure |
| Dev Tools | Visual Studio 2022, SSMS, Git |

---

## ✨ Features

- **Product Catalogue** — browse, search & filter by category with response caching
- **Shopping Cart** — session-based cart with add, update, remove & discount code support
- **Product Customisation** — custom colour, text & logo options per product
- **User Authentication** — register, login, logout & profile via ASP.NET Core Identity
- **Role-Based Access** — Admin & Customer roles with policy-based authorisation
- **Order Processing** — full lifecycle from cart → checkout → confirmation → tracking
- **Admin Dashboard** — sales metrics, low-stock alerts, recent orders & inventory management
- **Real-Time Chat** — SignalR-powered live chat hub
- **Security** — CSRF protection, security headers middleware, account lockout & cookie hardening
- **Reporting** — sales reports, revenue breakdowns & inventory status
- **Unit Tests** — business logic tested with xUnit & Moq

---

## 🏗 Architecture

- **Pattern:** MVC (Model-View-Controller) with Repository & Service layers
- **Repository Pattern:** `IProductRepository`, `IOrderRepository`, `ICartRepository` — data access abstracted from controllers
- **Service Layer:** `IOrderService`, `INotificationService`, `IReportService` — business logic isolated from controllers
- **Middleware:** Custom request logging middleware & security headers middleware
- **Filters:** Global exception filter & logging action filter
- **Database:** Relational schema with EF Core code-first migrations & seed data
- **Session:** Shopping cart persisted in server-side session as JSON

---

## 🔐 Default Admin Account

On first run the application automatically seeds a default admin user:

| Field | Value |
|---|---|
| Email | `admin@smartgear.com` |
| Password | `Admin@123456` |

> Change this password immediately after first login in a production environment.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server LocalDB
- [Visual Studio 2022](https://visualstudio.microsoft.com/)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/Ang3loSP/SmartGear-Online.git
   cd SmartGear-Online
   ```

2. **Configure the database connection**

   Update `appsettings.json` with your SQL Server instance:
   ```json
   "ConnectionStrings": {
     "SmartGearConnection": "Server=(localdb)\\mssqllocaldb;Database=SmartGearDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False"
   }
   ```

3. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```
   Navigate to `https://localhost:{port}` in your browser.

   > Alternatively open `SmartGear Online.sln` in Visual Studio & press **F5**.

---

## 📁 Project Structure

```
SmartGear Online/
├── Controllers/
│   ├── API/                  # REST API controllers (ProductsAPI)
│   ├── AccountController.cs  # Register, login, logout, profile
│   ├── AdminController.cs    # Admin dashboard & inventory (Admin role only)
│   ├── CartController.cs     # Session-based cart management
│   ├── CustomizationController.cs
│   ├── HomeController.cs
│   ├── OrderController.cs    # Checkout, confirmation, tracking, history
│   └── ProductController.cs  # Product listing, details, search, CRUD
├── Data/
│   └── ApplicationDbContext.cs
├── Extensions/
│   └── SessionExtensions.cs  # JSON session helpers
├── Filters/
│   ├── GlobalExeptionFilter.cs
│   └── LoggingActionsFilter.cs
├── Hubs/
│   ├── ChatHub.cs
│   └── InventoryHub.cs
├── Middleware/
│   ├── RequestPathLoggingMidddleware.cs
│   └── Security/SecurityHeadersMiddleware.cs
├── Migrations/               # EF Core migrations
├── Models/
│   ├── ViewModels/           # CheckoutViewModel, LoginViewModel, etc.
│   ├── ApplicationUser.cs
│   ├── CartItem.cs
│   ├── Category.cs
│   ├── Customization.cs
│   ├── Inventory.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── Product.cs
│   └── ShoppingCart.cs
├── Repositories/             # Data access layer
├── Services/                 # Business logic layer
├── Views/                    # Razor views (.cshtml)
├── wwwroot/                  # Static assets (CSS, JS, lib)
├── appsettings.json
└── Program.cs
```

---

## 🧪 Running Tests

```bash
dotnet test
```

Unit tests cover core business logic including cart calculations, order validation, product inventory rules & data access.

---

## ☁ Deployment

The application is configured for deployment to **Microsoft Azure App Service**. Update the connection string in `appsettings.json` (or via Azure App Service environment variables) before deploying.

---

## 🐛 Known Fixes Applied

The following bugs were identified & resolved during development:

| # | Bug | Fix |
|---|---|---|
| 1 | Mixed namespaces (`SmartGearOnline.*` vs `SmartGear_Online.*`) | Standardised all namespaces to `SmartGear_Online.*` |
| 2 | Session key mismatch (`"Cart"` vs `"ShoppingCart"`) | Unified to `"ShoppingCart"` across `CartController` & `OrderController` |
| 3 | `[ApiController]` on MVC `ProductController` | Removed — breaks Razor view rendering |
| 4 | `AdminController.Dashboard()` passed no model to view | Built & passed `AdminDashboardViewModel` with live data |
| 5 | `GetAllOrdersAsync` missing from `IOrderRepository` | Added interface method & implementation |
| 6 | `Register.cshtml` missing `ValidationSummary` | Added — Identity errors were silently swallowed |
| 7 | Phone & password regex too restrictive on registration | Relaxed to accept SA numbers & any special character |
| 8 | Nullable reference warnings across Models & Services | Added `= string.Empty` defaults & nullable `?` navigation properties |

---

## 📚 Academic Context

**Programme:** Higher Certificate in Full Stack Web & Software Development — NQF Level 5
**Institution:** Academic Institute of Excellence (AIE)
**Lecturer:** Brendon Magwagwa
**Year:** 2025 – 2026

**Competencies demonstrated:**
- C# & .NET 8 backend development
- OOP design principles & SOLID architecture
- Relational database design & EF Core
- MVC architecture with Repository & Service patterns
- ASP.NET Core Identity & role-based security
- Real-time features with SignalR
- Software testing with xUnit & Moq
- Cloud deployment on Microsoft Azure

---

## 👤 Author

**Angelo Puza**
📍 Johannesburg, Gauteng, South Africa
📧 spuza218@gmail.com
🔗 [GitHub](https://github.com/Ang3loSP) | [Portfolio](https://angelo-puza-portfolio.netlify.app)

---

*Project completed as part of ongoing coursework — actively maintained.*
