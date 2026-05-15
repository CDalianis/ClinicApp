# ClinicApp

A server-side rendered (SSR) clinic management web application built with **ASP.NET Core MVC**. ClinicApp manages doctors and application users, enforces authentication with **ASP.NET Core Identity**, and applies **capability-based authorization** so that each role can perform only the operations it is permitted to perform.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Getting Started](#getting-started)
- [Authentication and Authorization](#authentication-and-authorization)
- [Application Routes](#application-routes)
- [Logging](#logging)
- [Database](#database)

---

## Overview

ClinicApp is designed as a traditional MVC application: controllers process HTTP requests, Razor views render HTML on the server, and **Entity Framework Core** persists domain data to **MySQL**. On startup, the application applies pending EF Core migrations and seeds default roles, capability claims, regions, and a development administrator account.

The primary domain entity is **Doctor**, associated with a **Region**. Users authenticate via cookie-based sign-in; protected doctor operations require both authentication and the appropriate capability claim.

---

## Features

### Doctor management

| Capability | Description |
|------------|-------------|
| **List doctors** (`VIEW_DOCTORS`) | Paginated listing (default page size: 5, configurable up to 100), ordered by last name then first name, including region name |
| **Insert doctor** (`INSERT_DOCTOR`) | Create form with license number, first name, last name, and region; server-side validation and duplicate license check |
| **Edit doctor** (`EDIT_DOCTOR`) | Update existing doctor by UUID; uniqueness validation on license number |
| **Delete doctor** (`DELETE_DOCTOR`) | Soft delete (records are retained with `IsDeleted` flag; global query filter excludes deleted rows from normal queries) |

Additional doctor-related behavior:

- Unique **license number** and **UUID** per doctor
- **Audit fields** on entities: `CreatedAt`, `UpdatedAt`, `DeletedAt`
- Success pages after insert, update, and delete operations
- Structured information logging for create, update, and delete actions

### User management

- **Self-service registration** at `/users/register` (anonymous)
- Password confirmation and ASP.NET Identity password rules (minimum 8 characters, digit, uppercase, lowercase)
- Username uniqueness validation
- New users are assigned a role (`ADMIN` or `EMPLOYEE`); unauthenticated or non-admin registrants are restricted to **EMPLOYEE** to prevent privilege escalation
- Registration success page displaying the new user's UUID

### Authentication

- Cookie-based **login** and **logout** (`/login`, `/logout`)
- Optional “remember me” on sign-in
- **Access denied** page for unauthorized requests (`/access-denied`)
- Anti-forgery tokens on POST actions

### Authorization

- Two seeded roles: **ADMIN** and **EMPLOYEE**
- Fine-grained **capabilities** stored as role claims (`capability` claim type)
- Authorization policies registered for each capability; controllers use `[Authorize(Policy = ...)]`
- Default seeded administrator for local development (see [Authentication and Authorization](#authentication-and-authorization))

### Data and infrastructure

- **EF Core migrations** applied automatically on application startup
- **MySQL** connection configured via environment variables or defaults
- Seeded **regions** (Athens, Thessaloniki, Patras) for doctor assignment
- **Serilog** structured logging to console and rolling log files

### Presentation

- Server-rendered **Razor views** with Bootstrap-based layout
- Client-side validation via jQuery Validation and unobtrusive adapters
- Home page with navigation to doctors, login, and registration

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 |
| Web framework | ASP.NET Core MVC (Razor Views) |
| Authentication | ASP.NET Core Identity (cookie authentication) |
| ORM | Entity Framework Core 10 |
| Database | MySQL (`MySql.EntityFrameworkCore`) |
| Migrations | EF Core Migrations |
| Logging | Serilog (Console, File, Async sinks) |

---

## Project Structure

```
ClinicApp NET/
├── ClinicApp.slnx                 # Solution entry point
├── README.md
└── src-dotnet/
    └── ClinicApp.Web/
        ├── Controllers/           # MVC controllers (Account, Doctors, Home, Users)
        ├── Data/                  # DbContext, Identity user, migrations, seed data
        ├── Infrastructure/        # Shared types (e.g. PagedResult<T>)
        ├── Models/                # Domain entities (Doctor, Region, BaseEntity)
        ├── ViewModels/            # Input models for forms
        ├── Views/                 # Razor views
        ├── wwwroot/               # Static assets (CSS, JS, client libraries)
        ├── Program.cs             # Application startup and DI configuration
        └── appsettings.json       # Serilog and default configuration
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL Server 8.x (or compatible) reachable from the development machine
- (Optional) [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) for creating migrations locally:

  ```bash
  dotnet tool restore --project src-dotnet/ClinicApp.Web
  ```

---

## Configuration

Database connectivity is built from the following environment variables. If a variable is not set, the default in parentheses is used.

| Variable | Default | Description |
|----------|---------|-------------|
| `MYSQL_HOST` | `localhost` | MySQL server host |
| `MYSQL_PORT` | `3306` | MySQL server port |
| `MYSQL_DB` | `clinicapp` | Database name |
| `MYSQL_USER` | `clinic` | Database user |
| `MYSQL_PASSWORD` | `12345` | Database password |

Ensure the database exists and the user has rights to create and modify schema (migrations run on startup).

Serilog settings (minimum levels, file paths, retention) are defined in `src-dotnet/ClinicApp.Web/appsettings.json`. Development-specific overrides may be placed in `appsettings.Development.json`.

---

## Getting Started

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd "ClinicApp NET"
   ```

2. **Create the MySQL database and user** (example)

   ```sql
   CREATE DATABASE clinicapp CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   CREATE USER 'clinic'@'localhost' IDENTIFIED BY '12345';
   GRANT ALL PRIVILEGES ON clinicapp.* TO 'clinic'@'localhost';
   FLUSH PRIVILEGES;
   ```

3. **Set environment variables** (optional if using defaults above)

4. **Run the application**

   ```bash
   dotnet run --project src-dotnet/ClinicApp.Web/ClinicApp.Web.csproj
   ```

   Or open `ClinicApp.slnx` in Visual Studio / Rider and start the **https** or **http** profile.

5. **Open the site**

   - HTTP: `http://localhost:5219`
   - HTTPS: `https://localhost:7212`

   Migrations and seed data run automatically on first launch.

---

## Authentication and Authorization

### Default administrator (development)

After seeding, a default admin account is available if it did not already exist:

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin123!` |
| Role | `ADMIN` |

Change or remove this account in production environments.

### Roles and capabilities

| Role | Capabilities |
|------|----------------|
| **ADMIN** | `VIEW_DOCTORS`, `INSERT_DOCTOR`, `EDIT_DOCTOR`, `DELETE_DOCTOR` |
| **EMPLOYEE** | `VIEW_DOCTORS`, `INSERT_DOCTOR`, `EDIT_DOCTOR` |

Capability constants are defined in `SeedData.Capabilities`. Each capability maps to an authorization policy of the same name.

### Password policy

- Minimum length: 8 characters
- Requires at least one digit, one uppercase letter, and one lowercase letter
- Special characters are not required

---

## Application Routes

| Method | Path | Access | Purpose |
|--------|------|--------|---------|
| GET | `/` | Public | Home page |
| GET | `/login` | Anonymous | Login form |
| POST | `/login` | Anonymous | Sign in |
| POST | `/logout` | Authenticated | Sign out |
| GET | `/access-denied` | Authenticated | Authorization failure |
| GET | `/users/register` | Anonymous | User registration form |
| POST | `/users/register` | Anonymous | Create user |
| GET | `/users/success` | Anonymous | Registration confirmation |
| GET | `/doctors` | `VIEW_DOCTORS` | Paginated doctor list (`page`, `size` query parameters) |
| GET | `/doctors/insert` | `INSERT_DOCTOR` | Insert form |
| POST | `/doctors/insert` | `INSERT_DOCTOR` | Create doctor |
| GET | `/doctors/success` | `INSERT_DOCTOR` | Insert confirmation |
| GET | `/doctors/edit/{uuid}` | `EDIT_DOCTOR` | Edit form |
| POST | `/doctors/edit` | `EDIT_DOCTOR` | Update doctor |
| GET | `/doctors/update-success` | `EDIT_DOCTOR` | Update confirmation |
| POST | `/doctors/delete/{uuid}` | `DELETE_DOCTOR` | Soft delete doctor |
| GET | `/doctors/delete-success` | `DELETE_DOCTOR` | Delete confirmation |

---

## Logging

Serilog writes to:

| Destination | Path / channel | Content |
|-------------|----------------|---------|
| Console | Standard output | General application logs |
| File | `logs/all.log` | All levels (daily rolling, 30 files retained) |
| File | `logs/error.log` | Error and above |
| File | `logs/sql.log` | EF Core database commands (Information level) |

HTTP request logging is enabled via `UseSerilogRequestLogging()`. Log files are created relative to the application working directory and are excluded from source control via `.gitignore`.

---

## Database

### Main tables

| Table | Description |
|-------|-------------|
| `doctors` | Doctor records (UUID, license number, names, region, soft-delete flags, timestamps) |
| `regions` | Reference regions for doctors |
| `users` | Identity users (includes application `Uuid`) |
| `roles`, `user_roles`, `user_claims`, `role_claims`, `user_logins`, `user_tokens` | ASP.NET Identity schema |

### Migrations

Migrations live under `src-dotnet/ClinicApp.Web/Data/Migrations/`. To add a new migration after model changes:

```bash
cd src-dotnet/ClinicApp.Web
dotnet ef migrations add <MigrationName>
```

The application applies migrations automatically at startup; manual `dotnet ef database update` is optional for local workflows.

---

## License

This project is provided for educational and demonstration purposes. Specify a license here if the repository is distributed under particular terms.
