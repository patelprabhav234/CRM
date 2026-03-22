📘 SOFTWARE REQUIREMENT SPECIFICATION (SRS)
Product Name: FireOps CRM (by MaX)
1. 🎯 Objective
Build a Service + AMC + Sales CRM for fire safety companies to:

Manage leads and clients

Track installations

Automate AMC lifecycle

Handle service requests

Improve compliance tracking

2. 👥 User Roles
Role	Permissions
Admin	Full access
Sales	Leads, Quotes
Technician	Tasks, Services
Manager	Dashboard, Reports
3. 📦 Functional Requirements
3.1 Lead Management
Create/Edit/Delete Leads

Lead status pipeline

Assign to sales rep

3.2 Customer & Site Management
One customer → multiple sites

Store compliance data

Geo location (future)

3.3 Quotation System
Add products dynamically

Generate PDF

Track status

3.4 Installation Module
Assign technician

Upload images

Checklist completion

3.5 AMC Management (CORE)
Contract lifecycle

Auto reminders

Renewal alerts

Visit scheduling

3.6 Service Requests
Complaint logging

Assign technician

Track SLA

3.7 Task & Calendar
Daily task view

AMC visit planning

3.8 Notifications
Email / WhatsApp (future)

System alerts

4. 🏗️ SYSTEM ARCHITECTURE
🔷 High-Level Architecture
[ React Frontend ]
        ↓
[ API Gateway / .NET Web API ]
        ↓
[ Application Layer ]
        ↓
[ Domain Layer ]
        ↓
[ Infrastructure Layer ]
        ↓
[ PostgreSQL DB ]
🔧 Backend (.NET)
Architecture Pattern
Clean Architecture (Recommended)

CQRS (optional phase 2)

Layers
1. API Layer
Controllers

Authentication (JWT)

2. Application Layer
Services

DTOs

Business logic

3. Domain Layer
Entities

Enums

Interfaces

4. Infrastructure
EF Core

Repository Pattern

External integrations

💻 Frontend (React)
Stack
React + TypeScript

State: Redux Toolkit / Zustand

UI: Material UI / Tailwind

Structure
src/
 ├── components/
 ├── pages/
 ├── services/
 ├── store/
 ├── hooks/
 ├── layouts/
 └── styles/
5. 🗄️ DATABASE SCHEMA (Production Ready)
Users
Id (PK)
Name
Email
PasswordHash
Role
CreatedAt
Customers
Id (PK)
Name
ContactPerson
Phone
Email
Address
CreatedAt
Sites
Id (PK)
CustomerId (FK)
Name
Address
City
State
ComplianceStatus
Leads
Id (PK)
Name
Company
Phone
Email
Requirement
Status
AssignedTo
CreatedAt
Products
Id (PK)
Name
Category
Price
Description
Quotations
Id (PK)
CustomerId
SiteId
TotalAmount
Status
CreatedAt
QuotationItems
Id (PK)
QuotationId
ProductId
Quantity
Price
AMCContracts
Id (PK)
CustomerId
SiteId
StartDate
EndDate
VisitFrequency
Status
AMCVisits
Id (PK)
AMCId
ScheduledDate
CompletedDate
TechnicianId
Status
ServiceRequests
Id (PK)
CustomerId
SiteId
Description
Status
AssignedTo
CreatedAt
Tasks
Id (PK)
Title
AssignedTo
DueDate
Status
Type (AMC / Service / Installation)
6. 🖥️ UI REQUIREMENTS
Core Screens
1. Dashboard
KPI cards:

Active AMC

Pending services

Revenue

Charts

2. Leads Page
Table view

Kanban pipeline

Quick actions

3. Customers
List + detail page

Tabs:

Sites

AMC

Services

4. Quotations
Create form

Line items

PDF preview

5. AMC Module
List of contracts

Calendar view

Alerts

6. Service Requests
Ticket system UI

7. Technician App View (Mobile-friendly)
Tasks list

Upload images

Complete job

7. 🎨 UI/UX DESIGN SYSTEM (Professional)
Design Principles
Clean (like SaaS tools)

Fast navigation

Minimal clicks

Mobile-friendly

Color Palette
Primary: #D32F2F (Fire Red)
Secondary: #1E293B (Dark Blue)
Background: #F8FAFC
Success: #22C55E
Warning: #F59E0B
Typography
Font: Inter / Roboto

Headings: Bold

Body: Regular

Components
Reusable:

Buttons

Cards

Tables

Modals

Forms

CSS Strategy
Option 1 (Recommended)
Tailwind CSS + Design tokens

Option 2
SCSS with BEM naming

Layout
[Sidebar]
   |
   |-- Dashboard
   |-- Leads
   |-- Customers
   |-- AMC
   |-- Services
   |-- Reports

[Top Navbar]
   - Search
   - Notifications
   - Profile
8. 🔐 Non-Functional Requirements
Secure (JWT + role-based access)

Scalable (multi-tenant ready later)

Fast (<2 sec load)

Mobile responsive

9. 🚀 FUTURE ENHANCEMENTS
WhatsApp integration

AI lead scoring

Voice assistant

IoT integration (fire devices)

Multi-tenant SaaS

🔥 Final Insight
This is not just a CRM.

👉 This is a vertical SaaS for fire safety companies

If built right:

First client: Shah Fire Safety

Next 50 clients: Gujarat fire vendors

Then: National scale