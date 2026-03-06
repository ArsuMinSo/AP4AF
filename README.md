# UTB Minute – Canteen Ordering System

Semester project for the **Application Frameworks** course.

## Overview

The goal of this project is to design and implement a canteen ordering system using **.NET Aspire, Minimal WebAPI, Entity Framework Core and Blazor**.

The system allows students to order à-la-carte meals via a web application on a touch panel. Kitchen staff can then manage orders and update their status in real time.

## 📋 Project TODOs / Issues

See [TODOS.md](TODOS.md) for the full list of planned GitHub issues and work items.

## Solution Structure

| Project | Description |
|---------|-------------|
| `UTB.Minute.AppHost` | .NET Aspire orchestration |
| `UTB.Minute.Db` | Entities and DbContext |
| `UTB.Minute.DbManager` | HTTP commands for database management |
| `UTB.Minute.Contracts` | DTOs (Data Transfer Objects) |
| `UTB.Minute.WebApi` | Shared Minimal WebAPI with SSE |
| `UTB.Minute.WebApi.Tests` | Integration tests using PostgreSQL |
| `UTB.Minute.AdminClient` | Blazor Server app for canteen management |
| `UTB.Minute.CanteenClient` | Blazor Server app for students and cooks |

## Prerequisites

- Visual Studio 2026
- .NET 10 SDK
- Docker (for PostgreSQL and Keycloak via Aspire)

## Getting Started

1. Clone this repository
2. Open the solution in Visual Studio 2026
3. Set `UTB.Minute.AppHost` as the startup project
4. Run the application (Docker must be running)
5. Use the Aspire dashboard to access individual services
6. Use the HTTP command in Aspire to reset and seed the database
