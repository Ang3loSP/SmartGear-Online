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
- **Testing (in progress)** — `xUnit` & `Moq` referenced, test project not yet added

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

On first run, the application seeds a default admin account using a password
read from .NET user-secrets — **the password is never stored in source control**.

| Field | Value |
|---|---|
| Email | `admin@smartgear.com` |
| Password | *Set via `dotnet user-secrets`, see Setup step 3 below* |

> If `SeedAdmin:Password` is not configured, the app will skip admin seeding
> and print a warning with the exact command to run.

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

3. **Set the admin seed password (required)**

   The default admin account password is never stored in source control.
   Set it locally with .NET user-secrets:
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "SeedAdmin:Password" "YourStrongPassword123!"
   ```

4. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
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
│   ├── GlobalExceptionFilter.cs
│   └── LoggingActionsFilter.cs
├── Hubs/
│   ├── ChatHub.cs
│   └── InventoryHub.cs
├── Middleware/
│   ├── RequestPathLoggingMiddleware.cs
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

## 🧪 Testing

> **Status: in progress.** The project references `xUnit` and `Moq` in
> `SmartGear Online.csproj`, but a dedicated test project has not been added
> yet. Planned coverage includes cart total calculations, discount code
> validation, and order status transitions.

Once test files are added, run:
```bash
dotnet test
```

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
| 9 | Customization items silently lost (empty `ICartRepository`) | `CustomizationController` now saves directly to the session cart |
| 10 | Discount codes validated client-side only | Validation moved server-side via `IOrderService.ApplyDiscountAsync` |
| 11 | Admin seed password hardcoded in source | Read from `dotnet user-secrets` (`SeedAdmin:Password`) |
| 12 | Race condition in `ChatHub` typing indicator | `Dictionary` → `ConcurrentDictionary` |
| 13 | `AddProduct`/`EditProduct` links in Inventory pointed to missing views/actions | Added `AddProduct.cshtml`; `EditProduct` now redirects to the existing `Product/Edit` |
| 14 | Homepage contact modal had no working Send button | Added `HomeController.ContactAjax` JSON endpoint + fetch() wiring |
| 15 | Dead `SearchSuggestions` AJAX call (404 on every keystroke) | Added the missing controller action with debounced client-side calls |

> **Note on product images:** the images referenced in seed data and views
> (`/images/products/*.jpg`, `/images/hero-bg-stadium.jpg`) currently ship as
> placeholder graphics. Replace them with real photography before a public
> demo — see the Image Guidelines section below.

---

## 🖼 Image Guidelines

Placeholder images currently ship in `wwwroot/images/`. To replace them with
real photography:

| File | Used for | What to look for |
|---|---|---|
| `images/hero-bg-stadium.jpg` | Homepage hero background | Wide stadium/field shot, dark or duotone tone (text overlays on top), 1920×700px or larger, landscape |
| `images/products/jersey1.jpg` | Featured jersey card | Clean product shot of a single jersey, plain/white background or worn on a mannequin, square-ish (800×800–800×600) |
| `images/products/shoe1.jpg` | Featured shoe card | Side-angle sports shoe product shot, plain background, same aspect ratio as above |
| `images/products/gear1.jpg` | Featured gear card | Training/sports equipment flat-lay or product shot (balls, gloves, bags) |

**Sourcing options:**
- **Free stock photography:** [Unsplash](https://unsplash.com) and [Pexels](https://pexels.com) — search "sports jersey," "athletic shoes product shot," "sports equipment flat lay." Both are free for commercial/academic use, no attribution required (attribution still appreciated).
- **AI-generated:** consistent style across all four images, useful if you want a unified "brand" look rather than mixed stock photo styles.
- **Your own photography:** if you can photograph real jerseys/shoes/gear against a plain background, this gives the most authentic, original result for a portfolio piece.

Keep all product images at a consistent aspect ratio (the cards crop to
`height: 250px; object-fit: cover`, so slight cropping is fine, but avoid
extreme portrait or panoramic shots). Compress to under ~200KB each — large
unoptimized images will slow down page loads noticeably once you add more
than 3–4 products.

---

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
📧 support@smartgear.com
🔗 [GitHub](https://github.com/Ang3loSP) | [Portfolio](https://angelo-puza-portfolio.netlify.app)

---

*Project completed as part of ongoing coursework — actively maintained.*
