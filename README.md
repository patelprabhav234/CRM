# CRM Project

Welcome to the CRM project. This repository is organized into a clean, modern structure for professional development.

## 📂 Project Structure

- **`backend/`**: .NET 8 source code following Clean Architecture.
  - `CRM.Api`: REST API layer.
  - `CRM.Application`: Business logic and unit of work.
  - `CRM.Domain`: Core entities and interfaces.
  - `CRM.Infrastructure`: Database persistence, migrations, and identity.
- **`frontend/`**: React + TypeScript frontend built with Vite.
- **`docs/`**: Project documentation, including database migration guides and setup instructions.
- **`scripts/`**: Automation and utility scripts for development and testing.

## 🚀 Getting Started

### Backend
1. Open `CRM.slnx` at the root using Visual Studio or VS Code.
2. Configure your connection string in `backend/CRM.Api/appsettings.json`.
3. Run migrations using the guide in `docs/backend/migrations.md`.

### Frontend
1. Navigate to the `frontend/` directory.
2. Run `npm install`.
3. Start the dev server with `npm run dev`.

## 🛠 Documentation
Detailed guides can be found in the [docs/](./docs/) folder.
