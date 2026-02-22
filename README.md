# Study Tracker

A web application for tracking study sessions and competing with friends through leaderboards.

## Features

- **User Authentication**: Register, Login, Logout, and Password Management
- **Study Session Tracking**: Add, Edit, and Delete study sessions
- **Dashboard**: View daily, weekly, monthly, and all-time statistics
- **Weekly Target System**: Set daily study goals and track progress (Saturday to Saturday)
- **Leaderboards**: 
  - Daily Leaderboard (Card Layout with Top 3 Podium)
  - Weekly Leaderboard (Table Layout) - Primary
  - Monthly Leaderboard (Progress Style)
- **Admin Dashboard**: 
  - Manage users (Suspend/Unsuspend, Make Admin)
  - View and manage all study sessions (Edit/Delete)
  - View user targets and progress
- **Profile Management**: 
  - Edit name and change password
  - Upload and crop profile pictures
- **Notifications**: Modern modal notifications system (no browser alerts)
- **Streak System**: Track consecutive days of meeting study targets

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB)

### Setup

1. Clone the repository
2. Update the connection string in `appsettings.json` if needed
3. Run migrations:
   ```bash
   dotnet ef database update
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

### Default Admin Account

- Email: `admin@studytracker.com`
- Password: `Admin123!`

## Technology Stack

- ASP.NET Core MVC 8
- Entity Framework Core
- SQL Server
- ASP.NET Identity
- Tailwind CSS
- SixLabors.ImageSharp (Image processing)
- Cropper.js (Client-side image cropping)

## Project Structure

- `Controllers/` - MVC Controllers
- `Models/` - Domain Models and ViewModels
- `Services/` - Business Logic Services
- `Views/` - Razor Views
- `Data/` - DbContext and Data Seeding
- `Helpers/` - Utility Classes
