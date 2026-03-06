# 📋 Project TODOs / Issues

This file lists all planned GitHub issues for the **UTB Minute – Canteen Ordering System** project.

> Each section below corresponds to a GitHub Issue to be created in this repository.

---

## Issue #1 – Setup: Solution structure and project scaffolding

**Labels:** `setup`, `backend`
**Milestone:** Mid-semester submission

**Description:**
Create the .NET 10 solution with all required projects, correct naming, and project references as specified.

**Projects to create:**
- `UTB.Minute.AppHost` – .NET Aspire AppHost
- `UTB.Minute.Db` – Entities and DbContext
- `UTB.Minute.DbManager` – WebAPI for database HTTP commands (references `UTB.Minute.Db`)
- `UTB.Minute.Contracts` – DTOs (Data Transfer Objects)
- `UTB.Minute.WebApi` – Shared WebAPI for all clients incl. SSE (references `UTB.Minute.Db`, `UTB.Minute.Contracts`)
- `UTB.Minute.WebApi.Tests` – WebAPI tests using real PostgreSQL (references `UTB.Minute.WebApi`)
- `UTB.Minute.AdminClient` – Blazor Server for canteen management (references `UTB.Minute.Contracts`)
- `UTB.Minute.CanteenClient` – Blazor Server for students and cooks (references `UTB.Minute.Contracts`)

**Acceptance criteria:**
- [ ] All projects exist and are correctly named
- [ ] Project references match the specification
- [ ] Solution builds without errors or warnings
- [ ] Source code language is English

---

## Issue #2 – Backend: Data model and Entity Framework setup

**Labels:** `backend`, `database`
**Milestone:** Mid-semester submission

**Description:**
Design and implement the data model for the canteen ordering system using Entity Framework Core with PostgreSQL.

**Entities to create:**
- `Food` – id, name, description, price, is_active
- `MenuItem` – id, date, food_id (FK), available_portions
- `Order` – id, menu_item_id (FK), created_at, status (enum)

**Order status enum:**
- `Preparing` – portions count reduced
- `Ready` – ready for pickup
- `Cancelled` – order cancelled (portions NOT returned)
- `Completed` – picked up / student notified of cancellation

**Acceptance criteria:**
- [ ] All entities defined in `UTB.Minute.Db` project
- [ ] `AppDbContext` correctly configured with all entities and relationships
- [ ] Order status implemented as an enum
- [ ] EF Core migrations created and working with PostgreSQL
- [ ] Database can be created via Aspire

---

## Issue #3 – Backend: DTOs and Contracts layer

**Labels:** `backend`
**Milestone:** Mid-semester submission

**Description:**
Create Data Transfer Objects (DTOs) in the `UTB.Minute.Contracts` project. DTOs must be independent of entities and defined only once (no duplication).

**DTOs to create:**
- `FoodDto`, `CreateFoodDto`, `UpdateFoodDto`
- `MenuItemDto`, `CreateMenuItemDto`, `UpdateMenuItemDto`
- `OrderDto`, `CreateOrderDto`, `UpdateOrderStatusDto`

**Acceptance criteria:**
- [ ] All DTOs defined only in `UTB.Minute.Contracts`
- [ ] DTOs are independent of EF entities
- [ ] No entity types exposed directly through WebAPI
- [ ] Code is not duplicated across projects

---

## Issue #4 – Backend: Food management WebAPI endpoints

**Labels:** `backend`, `webapi`
**Milestone:** Mid-semester submission

**Description:**
Implement Minimal WebAPI endpoints for food management in `UTB.Minute.WebApi`.

**Endpoints:**
- `GET /foods` – list all foods
- `GET /foods/{id}` – get food by id
- `POST /foods` – create new food
- `PUT /foods/{id}` – update food
- `PATCH /foods/{id}/deactivate` – deactivate food (foods are never deleted, only deactivated)

**Acceptance criteria:**
- [ ] All endpoints implemented using Minimal WebAPI with TypedResults
- [ ] Returns DTOs, not entities
- [ ] Food deactivation sets `IsActive = false` (no hard delete)
- [ ] Endpoints have corresponding tests in `UTB.Minute.WebApi.Tests`

---

## Issue #5 – Backend: Menu management WebAPI endpoints

**Labels:** `backend`, `webapi`
**Milestone:** Mid-semester submission

**Description:**
Implement Minimal WebAPI endpoints for daily menu management in `UTB.Minute.WebApi`.

**Endpoints:**
- `GET /menu` – list all menu items (date, food, available portions)
- `GET /menu/{id}` – get menu item by id
- `GET /menu/today` – get today's menu items
- `POST /menu` – create menu item
- `PUT /menu/{id}` – update menu item
- `DELETE /menu/{id}` – delete menu item

**Acceptance criteria:**
- [ ] All endpoints implemented using Minimal WebAPI with TypedResults
- [ ] Returns DTOs, not entities
- [ ] Endpoints have corresponding tests in `UTB.Minute.WebApi.Tests`

---

## Issue #6 – Backend: Order management WebAPI endpoints

**Labels:** `backend`, `webapi`
**Milestone:** Mid-semester submission

**Description:**
Implement Minimal WebAPI endpoints for order management in `UTB.Minute.WebApi`.

**Endpoints:**
- `GET /orders` – list all non-completed orders (for cooks)
- `GET /orders/{id}` – get order by id
- `POST /orders` – create new order (reduces available portions)
- `PATCH /orders/{id}/status` – update order status

**Business rules:**
- Creating an order reduces `available_portions` on the menu item
- Cancelling an order does NOT restore portions
- Invalid status transitions must be rejected (e.g., cannot go from `Cancelled` to `Ready`)

**Valid status transitions:**
- `Preparing` → `Ready`
- `Preparing` → `Cancelled`
- `Ready` → `Completed`
- `Cancelled` → `Completed` (notification sent)

**Acceptance criteria:**
- [ ] All endpoints implemented using Minimal WebAPI with TypedResults
- [ ] Returns DTOs, not entities
- [ ] Invalid status transitions return appropriate error responses
- [ ] Endpoints have corresponding tests in `UTB.Minute.WebApi.Tests`

---

## Issue #7 – Backend: Database manager and HTTP commands

**Labels:** `backend`, `aspire`
**Milestone:** Mid-semester submission

**Description:**
Implement the `UTB.Minute.DbManager` WebAPI project with HTTP commands for database lifecycle management.

**Endpoints:**
- `POST /db/reset` – drop, recreate, and seed the database with test data

**Seed data should include:**
- Several sample foods
- Menu items for today and nearby dates
- Sample orders in various states

**Acceptance criteria:**
- [ ] `DbManager` project exists and references `UTB.Minute.Db`
- [ ] Reset endpoint drops and recreates the database
- [ ] Seed data populates the database with realistic test data
- [ ] Endpoint is accessible via Aspire HTTP commands

---

## Issue #8 – Aspire: Infrastructure setup and service discovery

**Labels:** `aspire`, `infrastructure`
**Milestone:** Mid-semester submission

**Description:**
Configure .NET Aspire in `UTB.Minute.AppHost` to orchestrate all services with proper service discovery and no hardcoded IP addresses.

**Requirements:**
- PostgreSQL database provisioned via Aspire
- All services registered with Aspire service discovery
- No hardcoded IP addresses or connection strings
- HTTP command configured for database reset

**Acceptance criteria:**
- [ ] PostgreSQL database created and configured via Aspire
- [ ] All projects (WebApi, DbManager, AdminClient, CanteenClient) registered in AppHost
- [ ] Service discovery works between all services
- [ ] No hardcoded IP addresses in any project
- [ ] Database reset HTTP command accessible from Aspire dashboard

---

## Issue #9 – Testing: WebAPI integration tests

**Labels:** `testing`
**Milestone:** Mid-semester submission

**Description:**
Implement automated integration tests for all WebAPI endpoints in `UTB.Minute.WebApi.Tests` using a real PostgreSQL database (not InMemory EF).

**Test coverage required:**
- Food endpoints: create, read, update, deactivate
- Menu endpoints: create, read, update, delete
- Order endpoints: create, read, status transitions
- Invalid status transition rejection

**Acceptance criteria:**
- [ ] Tests use real PostgreSQL (not InMemory EF)
- [ ] Tests run automatically without manual intervention
- [ ] Tests use database provisioned via Aspire
- [ ] All CRUD operations tested
- [ ] Invalid business rule violations tested (e.g., invalid order status transitions)

---

## Issue #10 – Frontend: Admin client (Blazor Server)

**Labels:** `frontend`, `blazor`
**Milestone:** Semester submission

**Description:**
Implement the `UTB.Minute.AdminClient` Blazor Server application for canteen management. Calls WebAPI via HTTP, does not access the database directly.

**Food management pages:**
- List of foods (name, description, price, active status)
- Create new food form
- Edit food form
- Deactivate food button

**Menu management pages:**
- List of menu items (date, food, available portions) for all days
- Create menu item form
- Edit menu item form
- Delete menu item button

**Acceptance criteria:**
- [ ] AdminClient is a Blazor Server application
- [ ] Calls `UTB.Minute.WebApi` via HTTP (no direct database access)
- [ ] References only `UTB.Minute.Contracts` (not `UTB.Minute.Db`)
- [ ] All CRUD operations for foods and menu items work
- [ ] Application accessible via Aspire dashboard

---

## Issue #11 – Frontend: Student ordering interface (CanteenClient)

**Labels:** `frontend`, `blazor`
**Milestone:** Semester submission

**Description:**
Implement the student-facing pages in `UTB.Minute.CanteenClient` for viewing today's menu and placing orders.

**Pages:**
- Today's menu – list of menu items with food name, description, price, available portions
  - Sold-out items visually distinguished (greyed out, badge, etc.)
- Order button – places order for selected food
- My orders – list of current orders with status

**Acceptance criteria:**
- [ ] Students can see today's menu
- [ ] Sold-out items are visually distinct from available items
- [ ] Ordering a food reduces available portions
- [ ] Students can see their order status in real time (via SSE)
- [ ] Calls WebAPI via HTTP only

---

## Issue #12 – Frontend: Cook order management interface (CanteenClient)

**Labels:** `frontend`, `blazor`
**Milestone:** Semester submission

**Description:**
Implement the cook-facing pages in `UTB.Minute.CanteenClient` for managing orders. Access must be secured (Keycloak role).

**Pages:**
- Active orders list – shows all non-completed orders
- Order detail – shows food name and creation time
- Status change buttons – mark order as Ready, Cancelled, or Completed

**Business rules:**
- Invalid status transitions must be blocked in the UI
- Cannot change status from `Cancelled` to `Ready`

**Acceptance criteria:**
- [ ] Cooks see all active (non-completed) orders
- [ ] Cooks can change order status (Ready / Cancelled / Completed)
- [ ] Invalid transitions are blocked (UI and API level)
- [ ] Order list updates in real time (via SSE)
- [ ] Accessible only to users with cook role

---

## Issue #13 – Feature: Server-Sent Events (SSE) for real-time notifications

**Labels:** `backend`, `frontend`, `feature`
**Milestone:** Semester submission

**Description:**
Implement Server-Sent Events in `UTB.Minute.WebApi` to push real-time order status updates to all connected clients (students and cooks) without authentication.

**SSE Endpoint:**
- `GET /orders/events` – SSE stream broadcasting all order status changes

**Events to broadcast:**
- Order created
- Order status changed (Preparing → Ready / Cancelled / Completed)

**Client integration:**
- Student UI auto-refreshes order status when SSE event received
- Cook UI auto-refreshes order list when SSE event received

**Acceptance criteria:**
- [ ] SSE endpoint implemented in WebApi
- [ ] Events broadcast to all connected clients without authentication
- [ ] Student UI updates automatically on order status change
- [ ] Cook UI updates automatically when new orders arrive or statuses change
- [ ] SSE works across all supported browsers

---

## Issue #14 – Security: Keycloak authentication and authorization

**Labels:** `security`, `aspire`
**Milestone:** Semester submission

**Description:**
Integrate Keycloak via Aspire for authentication and role-based authorization.

**Roles:**
- `admin` – canteen management (AdminClient)
- `cook` – order management (CanteenClient cook view)
- `student` – ordering (CanteenClient student view)

**Requirements:**
- Keycloak provisioned and configured via Aspire
- Backend endpoints secured by role
- UI adapts to logged-in user's role

**Acceptance criteria:**
- [ ] Keycloak running via Aspire (no manual setup required)
- [ ] WebAPI endpoints protected based on role
- [ ] AdminClient accessible only to `admin` role
- [ ] Cook pages accessible only to `cook` role
- [ ] Student ordering accessible to `student` role
- [ ] UI shows/hides functionality based on role
- [ ] Instructor can run the full project locally (VS 2026, .NET 10, Docker)

---

## Issue #15 – Feature: Concurrency handling for last portion ordering

**Labels:** `backend`, `feature`
**Milestone:** Semester submission

**Description:**
Implement database-level concurrency control to handle race conditions when multiple students try to order the last available portion simultaneously.

**Implementation options:**
- Optimistic concurrency with `RowVersion` / ETag on `MenuItem`
- Pessimistic locking via database transaction
- Application-level lock with retry logic

**Acceptance criteria:**
- [ ] Two simultaneous orders for the last portion result in only one successful order
- [ ] Second request receives appropriate error response (e.g., 409 Conflict)
- [ ] Implementation uses database-level or transaction-level concurrency (not application-level only)
- [ ] Test demonstrates correct behavior under concurrent access

---

## Issue #16 – Documentation: README.md

**Labels:** `documentation`
**Milestone:** Mid-semester submission (basic) / Semester submission (full)

**Description:**
Write comprehensive project documentation in `README.md` following the provided template.

**Required sections:**
- Project overview and architecture decisions
- How to run the project locally (prerequisites: VS 2026, .NET 10, Docker)
- Project structure explanation
- Known issues or problems encountered
- Team member contribution ratio (e.g., `1:1:1` for equal contributions)

**Acceptance criteria:**
- [ ] README.md exists and is written in Markdown
- [ ] Covers all required sections
- [ ] Architecture decisions explained
- [ ] Prerequisites and run instructions clear
- [ ] Team contribution ratio included

---

## Issue #17 – Bonus: Avalonia desktop client for cooks

**Labels:** `bonus`, `frontend`
**Milestone:** Optional

**Description:**
Implement a desktop application for cooks using the [Avalonia](https://avaloniaui.net/) framework as a bonus task.

**Requirements:**
- Displays active orders
- Allows status changes (Ready / Cancelled / Completed)
- Uses the same WebAPI as other clients
- Real-time updates via SSE

**Acceptance criteria:**
- [ ] Avalonia desktop app builds and runs on Windows
- [ ] Connects to `UTB.Minute.WebApi` via HTTP
- [ ] All cook functionality available
- [ ] Real-time updates working via SSE

---

## Summary

| Issue | Title | Milestone | Labels |
|-------|-------|-----------|--------|
| #1 | Solution structure and project scaffolding | Mid-semester | setup, backend |
| #2 | Data model and Entity Framework setup | Mid-semester | backend, database |
| #3 | DTOs and Contracts layer | Mid-semester | backend |
| #4 | Food management WebAPI endpoints | Mid-semester | backend, webapi |
| #5 | Menu management WebAPI endpoints | Mid-semester | backend, webapi |
| #6 | Order management WebAPI endpoints | Mid-semester | backend, webapi |
| #7 | Database manager and HTTP commands | Mid-semester | backend, aspire |
| #8 | Aspire infrastructure setup and service discovery | Mid-semester | aspire, infrastructure |
| #9 | WebAPI integration tests | Mid-semester | testing |
| #10 | Admin client (Blazor Server) | Semester | frontend, blazor |
| #11 | Student ordering interface (CanteenClient) | Semester | frontend, blazor |
| #12 | Cook order management interface (CanteenClient) | Semester | frontend, blazor |
| #13 | SSE real-time notifications | Semester | backend, frontend, feature |
| #14 | Keycloak authentication and authorization | Semester | security, aspire |
| #15 | Concurrency handling for last portion ordering | Semester | backend, feature |
| #16 | Documentation: README.md | Both milestones | documentation |
| #17 | Avalonia desktop client for cooks (bonus) | Optional | bonus, frontend |
