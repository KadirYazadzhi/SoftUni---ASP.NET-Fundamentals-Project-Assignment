# 🔨 AuctionHub

![Build Status](https://img.shields.io/badge/Build-Passing-success?style=for-the-badge&logo=github)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![Status](https://img.shields.io/badge/Status-Active%20Development-blue?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

> **Note:** This project is developed as part of the SoftUni ASP.NET Fundamentals course.

## 📖 Overview

**AuctionHub** is a sophisticated online auction marketplace designed to deliver a premium user experience. It bridges the gap between buyers and sellers with a modern, secure, and intuitive platform.

Unlike standard template-based projects, AuctionHub features a completely **custom-designed user interface** focused on usability ("Glassmorphism" aesthetic), responsiveness, and smooth micro-interactions. It demonstrates a robust implementation of relational data models within a clean **ASP.NET Core MVC** architecture.

---

## ✨ Key Features & Highlights

### 🎨 Modern User Interface (New in v2.0)
The application has undergone a complete visual overhaul to meet 2026 web standards:
*   **Glassmorphism Design:** Translucent navigation bars and frosted glass effects for a sleek, airy feel.
*   **Inter Typography:** Utilizing the 'Inter' font family for professional-grade readability.
*   **Interactive Elements:** Smooth `fade-in-up` animations, `hover-lift` card effects, and soft shadows.
*   **Dark Mode Support:** Fully integrated, automatic dark theme that respects user preferences.
*   **Responsive Layout:** Optimized for all screen sizes using Bootstrap 5 grid system.

### 👤 Identity & Security
*   **Custom Auth Pages:** Login and Register pages are fully customized to match the site's brand, moving away from the default Identity UI.
*   **Role-Based Access:** 
    *   **Guests:** Can browse auctions and search.
    *   **Users:** Can create listings, place bids, and manage their profile.
    *   **Admins:** (Coming Soon) Can moderate content.
*   **Data Protection:** Secure password hashing and protection against CSRF/XSS attacks.

### 🛒 Auction Ecosystem
*   **Create Listings:** Comprehensive form with validation for Title, Description, Start Price, Image URL, and End Date.
*   **Smart Categorization:** Items are organized into visual categories (Electronics, Art, Antiques, Vehicles) for easy discovery.
*   **My Auctions:** dedicated dashboard for sellers to track their active listings.
*   **My Bids:** dedicated dashboard for buyers to see all auctions they are participating in.

### 💸 Bidding Logic (Business Rules)
*   **Validation:**
    *   Bids must be strictly higher than the current price.
    *   **Self-Bidding Prevention:** Sellers cannot bid on their own auctions.
    *   **Time Check:** Bids are rejected if the auction end time has passed.
*   **Real-Time Updates:** The "Current Price" updates instantly upon a successful bid.
*   **History:** A transparent list of the last 5 bids is displayed on the auction details page.

---

## 💾 Data Model

The application uses a relational database designed with **Entity Framework Core Code-First**:

*   **ApplicationUser:** Extends `IdentityUser`. Has collections of `MyAuctions` and `MyBids`.
*   **Auction:** The core entity.
    *   `SellerId` (FK to User)
    *   `CategoryId` (FK to Category)
    *   `CurrentPrice` / `StartPrice`
    *   `EndTime`
*   **Bid:** Represents a transaction attempt.
    *   `BidderId` (FK to User)
    *   `AuctionId` (FK to Auction)
    *   `Amount` / `Timestamp`
*   **Category:** Grouping entity (`Name`, `Auctions` collection).

---

## 🛠️ Tech Stack

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Backend** | ASP.NET Core 8.0 | High-performance, cross-platform framework. |
| **Language** | C# 12 | Using latest language features. |
| **ORM** | Entity Framework Core | Database access and migration management. |
| **Database** | Microsoft SQL Server | Reliable relational database storage. |
| **Frontend** | Razor Views + Bootstrap 5 | Server-side rendering with responsive styling. |
| **Design** | Custom CSS + Google Fonts | "Inter" font and Glassmorphism custom styles. |
| **Icons** | Bootstrap Icons | Vector icons for UI elements. |

---

## 🚀 Getting Started

Follow these steps to set up the project locally.

### Prerequisites
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download) installed.
*   [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB or full instance).
*   A code editor like **Visual Studio 2022** or **VS Code**.

### Installation Steps

1.  **Clone the repository**
    ```bash
    git clone https://github.com/YourUsername/AuctionHub.git
    cd AuctionHub
    ```

2.  **Configure Database**
    Open `appsettings.json` and ensure the `DefaultConnection` string points to your SQL Server instance.
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=.;Database=AuctionHub;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
    }
    ```

3.  **Apply Migrations**
    This will create the database and the schema.
    ```bash
    dotnet ef database update
    ```
    *Note: The project includes a `DbSeeder` that will automatically populate initial Categories and Test Auctions upon first run.*

4.  **Run the Application**
    ```bash
    dotnet run
    ```
    Open your browser to `https://localhost:7000` (or the port shown in your terminal).

---

## 🔮 Roadmap (Future Improvements)

*   [ ] **Admin Panel:** Functionality to delete/edit any auction and manage users.
*   [ ] **Search & Filtering:** Advanced filters by price range and end date.
*   [ ] **Images Upload:** Replace Image URL with real file upload handling.
*   [ ] **SignalR Integration:** Live price updates without refreshing the page.
*   [ ] **Watchlist:** Ability to "star" auctions and get notifications.

## 🤝 Contributing

Contributions are welcome! Please fork the repository and create a Pull Request for any features or bug fixes.

## 📜 License

Distributed under the MIT License.
