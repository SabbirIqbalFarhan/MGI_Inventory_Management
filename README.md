# MGI_Inventory_Management
# MGI Inventory Management

A web-based inventory management system built with **ASP.NET (C#)**, designed to streamline product tracking, stock control, and inventory operations.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

MGI Inventory Management is a full-stack web application that provides a centralized platform for managing inventory data. It enables users to add, update, and track products, monitor stock levels, and generate reports — all through an intuitive web interface.

---

## Features

- 📦 Add, edit, and delete inventory items
- 🔍 Search and filter products by category, name, or status
- 📊 Track stock levels and receive low-stock alerts
- 🧾 View inventory history and transaction logs
- 👤 User authentication and role-based access control
- 📱 Responsive UI for desktop and mobile browsers

---

## Tech Stack

| Layer      | Technology              |
|------------|-------------------------|
| Backend    | C# / ASP.NET            |
| Frontend   | HTML, CSS, JavaScript   |
| IDE        | Visual Studio           |
| Build      | .NET Solution (`.sln`)  |

---

## Prerequisites

Before running this project, make sure you have the following installed:

- [Visual Studio 2019/2022](https://visualstudio.microsoft.com/) (with ASP.NET and web development workload)
- [.NET Framework / .NET SDK](https://dotnet.microsoft.com/download) (version compatible with the project)
- SQL Server or LocalDB (if a database is used)

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/SabbirIqbalFarhan/MGI_Inventory_Management.git
cd MGI_Inventory_Management
```

### 2. Open in Visual Studio

Open the solution file:

```
MGI_Inventory_Management.sln
```

### 3. Restore NuGet Packages

In Visual Studio, go to:

```
Tools → NuGet Package Manager → Restore NuGet Packages
```

Or via CLI:

```bash
dotnet restore
```

### 4. Configure the Database

- Update the connection string in `Web.config` or `appsettings.json` to point to your SQL Server instance.
- Run any database migrations or seed scripts if provided.

### 5. Build and Run

Press **F5** in Visual Studio, or run:

```bash
dotnet run
```

The application will be available at `https://localhost:{port}` in your browser.

---

## Project Structure

```
MGI_Inventory_Management/
│
├── MGI_Inventory_Management/       # Main project directory
│   ├── Controllers/                # Application controllers
│   ├── Models/                     # Data models
│   ├── Views/                      # Razor/HTML views
│   ├── Content/                    # CSS and static assets
│   └── Scripts/                    # JavaScript files
│
├── MGI_Inventory_Management.sln    # Visual Studio solution file
├── .gitignore
└── README.md
```

---

## Usage

1. **Login** with your credentials (admin or user role).
2. Navigate to the **Inventory** section to view all products.
3. Use the **Add Item** button to create a new inventory record.
4. **Edit** or **Delete** existing items as needed.
5. Use the **Search/Filter** bar to quickly find items.
6. Check the **Dashboard** for stock summaries and alerts.

---

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a new branch: `git checkout -b feature/your-feature-name`
3. Commit your changes: `git commit -m "Add your feature"`
4. Push to the branch: `git push origin feature/your-feature-name`
5. Open a Pull Request

---

## License

This project is open source and available under the [MIT License](LICENSE).

---

> Developed by [Sabbir Iqbal Farhan](https://github.com/SabbirIqbalFarhan)
