# 🔨 AuctionHub - Premium Digital Marketplace

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![Status](https://img.shields.io/badge/Status-Completed-success?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![MVC](https://img.shields.io/badge/Architecture-MVC%20%2B%20Services-blue?style=for-the-badge)

**AuctionHub** is a comprehensive online auction platform designed to provide a secure and engaging environment for trading unique items. Built with a focus on reliability and user experience, the system handles complex financial logic, real-time-like notifications, and advanced administration through a modern, responsive interface.

---

## 📖 Table of Contents
1. [Overview](#-overview)
2. [Key Features](#-key-features)
3. [Technical Stack](#-technical-stack)
4. [Project Architecture](#-architecture)
5. [Database Schema](#-database-model)
6. [Project Structure](#-directory-structure)
7. [Security & Integrity](#-security--data-integrity)
8. [Testing](#-testing)
9. [Installation](#-installation--setup)
10. [Roadmap](#-future-roadmap)

---

## 🧐 Overview
AuctionHub was created to solve the challenges of online bidding: transparency, security, and speed. The platform allows users to list items for auction, participate in competitive bidding, and manage their finances through an integrated secure wallet. It acts as an escrow between buyers and sellers, ensuring that funds are only transferred when the auction rules are met.

---

## 🌟 Key Features

### 1. Auction Ecosystem
*   **Dynamic Lifecycle**: Auctions transition through states (Active, Ended, Suspended) automatically based on time and administrative actions.
*   **Bidding Logic**: Supports standard bidding with minimum increments and a "Buy It Now" feature for immediate acquisition.
*   **Advanced Discovery**: Users can search and filter auctions by category, price range, and status.

### 2. Integrated Financial System
*   **Digital Wallet**: Each user maintains a balance used for bidding and receiving proceeds from sales.
*   **Automated Escrow**: Funds are deducted the moment a bid is placed and automatically refunded if the user is outbid.
*   **Transaction Ledger**: A complete, unchangeable history of every deposit, bid, refund, and purchase.

### 3. User Experience
*   **Custom Identity**: Bespoke registration and login pages featuring unique usernames and profile management.
*   **Notifications**: Internal system alerting users about outbid status, auction wins, or administrative updates.
*   **Watchlist**: Allows users to monitor specific items without participating in the bidding immediately.

### 4. Admin Management
*   **Dashboard**: High-level statistics on system health and economic activity.
*   **User Moderation**: Ability to lock accounts or manually adjust balances for support purposes.
*   **Content Control**: Global oversight of all listings with the ability to suspend auctions violating terms.

---

## 🛠 Technical Stack

### Backend
*   **C# 12 / .NET 8.0**: Latest language features and performance.
*   **ASP.NET Core MVC**: Robust routing and server-side rendering.
*   **Entity Framework Core**: Code-First approach for database management.
*   **Identity Framework**: Secured authentication and authorization.

### Frontend
*   **Razor Views**: Dynamic HTML generation.
*   **Bootstrap 5**: Responsive layout and base components.
*   **Custom CSS3**: Glassmorphism aesthetic, animations, and dark mode support.

---

## 🏗 Architecture
The project follows a **Layered Architecture** to ensure maintainability:
*   **Controllers**: Handle HTTP requests and manage navigation.
*   **Services**: Contain the core business logic (Bidding, Wallet transfers, Notifications).
*   **Data Models**: Represent the database structure and relationships.
*   **ViewModels**: Optimized data structures for specific UI views.
*   **Background Services**: Automated workers handling time-sensitive tasks like closing auctions.

---

## 💾 Database Model
*   **ApplicationUser**: Extends Identity with wallet balance and personal info.
*   **Auction**: Central entity containing pricing, timing, and status.
*   **Bid**: Represents individual bidding attempts.
*   **Category**: Categorization for better item discovery.
*   **Transaction**: Detailed log of financial movements.
*   **Notification**: Internal messaging system for users.
*   **ContactMessage**: Stores inquiries from the "About Us" form.

---

## 📁 Directory Structure
```text
AuctionHub/
├── Areas/
│   └── Admin/              # Admin-only controllers and views
├── Controllers/            # Main application controllers
├── Data/                   # DbContext and seeding logic
├── Migrations/             # Database version history
├── Models/                 # Database entities
│   └── ViewModels/         # UI-specific DTOs
├── Services/               # Business logic and background tasks
├── Views/                  # Razor HTML templates
└── wwwroot/                # Static assets (CSS, Images, JS)
```

---

## 🛡 Security & Data Integrity
*   **Optimistic Concurrency**: Uses `RowVersion` timestamps to prevent data loss during simultaneous bids.
*   **Soft Delete**: Administrative actions use status flags instead of permanent data deletion.
*   **Input Validation**: Strict server-side and client-side validation for all forms.
*   **Role-Based Access**: Granular control over user and administrator capabilities.

---

## 🧪 Testing
The logic is validated using a dedicated **xUnit** project:
*   **AuctionServiceTests**: Comprehensive coverage of bidding workflows.
*   **Mocking**: Uses `Moq` to simulate service dependencies.
*   **In-Memory DB**: Fast, isolated database testing without external dependencies.

---

## 🔧 Installation & Setup

1. **Prerequisites**: .NET 8 SDK and SQL Server.
2. **Clone**: `git clone https://github.com/YourUsername/AuctionHub.git`
3. **Database**: Update connection string in `appsettings.json`.
4. **Initialize**: 
   ```bash
   dotnet ef database update
   ```
5. **Run**: `dotnet run`

---

## 🔮 Future Roadmap
*   **Real-time Bidding**: Integrating SignalR for instant price updates.
*   **Payment Integration**: Moving from mock wallet to Stripe/PayPal.
*   **Rating System**: Trust-based reviews for buyers and sellers.
*   **Web API**: Providing data for mobile applications.

---
**Created by Kadir Yazadzhi** - *SoftUni ASP.NET Fundamentals Project*