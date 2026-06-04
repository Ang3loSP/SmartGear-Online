# SmartGear Online 🛒

> A fully functional e-commerce web application built with **ASP.NET Core 8 MVC** & **C#**, developed as part of the Full Stack Web & Software Development programme at the Academic Institute of Excellence (AIE).

---

## 📌 Overview

SmartGear Online is a production-style e-commerce platform covering the full development lifecycle — from database design & user authentication through to real-time features, unit testing, & cloud deployment.

This project was built to demonstrate practical application of .NET backend development, relational database management, & cloud deployment using Microsoft Azure.

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 MVC |
| Language | C# |
| Database | SQL Server |
| ORM | Entity Framework Core (code-first migrations) |
| Real-Time | SignalR |
| Auth | ASP.NET Core Identity |
| Testing | xUnit |
| Cloud | Microsoft Azure |
| Dev Tools | Visual Studio, SSMS, Git |

---

## ✨ Features

- **Product Catalogue** — browse & filter product listings
- **Shopping Cart** — add, update & remove items with session persistence
- **User Authentication** — register, login & role-based access via ASP.NET Core Identity
- **Order Processing** — full order lifecycle from cart to confirmation
- **Real-Time Updates** — live notifications using SignalR
- **Unit Tests** — core business logic validated with xUnit
- **Azure Deployment** — hosted on Microsoft Azure with CI/CD pipeline experience

---

## 🏗 Architecture

- **Pattern:** MVC (Model-View-Controller)
- **Database:** Relational schema designed with normalisation principles; managed via EF Core code-first migrations
- **Security:** Password hashing, role-based authorisation & input validation
- **Methodology:** SDLC principles & UML modelling applied throughout the development lifecycle

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server Express)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/SmartGear-Online.git
   cd SmartGear-Online
   ```

2. **Configure the database connection**

   Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=SmartGearDB;Trusted_Connection=True;"
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
   Then navigate to `https://localhost:5001` in your browser.

---

## 📁 Project Structure

```
SmartGear-Online/
├── Controllers/        # MVC controllers (Products, Cart, Orders, Account)
├── Models/             # Domain models & ViewModels
├── Views/              # Razor views (.cshtml)
├── Data/               # EF Core DbContext & migrations
├── Services/           # Business logic layer
├── Hubs/               # SignalR hubs
├── wwwroot/            # Static assets (CSS, JS, images)
└── Tests/              # xUnit test project
```

---

## 🧪 Running Tests

```bash
dotnet test
```

Unit tests cover core business logic including cart calculations, order processing rules & data validation.

---

## ☁ Deployment

The application is configured for deployment to **Microsoft Azure App Service**. CI/CD pipeline setup follows standard Azure DevOps / GitHub Actions patterns.

---

## 📚 Academic Context

**Programme:** Higher Certificate in Full Stack Web & Software Development — NQF Level 5
**Institution:** Academic Institute of Excellence (AIE)
**Year:** 2025 – Present

This project demonstrates competency in:
- C# & .NET backend development
- OOP design principles
- Relational database design & EF Core
- MVC architecture
- Cloud deployment & DevOps basics
- Software testing with xUnit

---

## 👤 Author

**Angelo Puza**
📍 Johannesburg, Gauteng, South Africa
📧 spuza218@gmail.com

---

*Project is actively in development as part of ongoing coursework.*
