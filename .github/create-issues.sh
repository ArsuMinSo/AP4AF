#!/usr/bin/env bash
# Script to create all project GitHub issues
# Usage: GITHUB_TOKEN=your_token ./create-issues.sh
# Or:    gh auth login && ./create-issues.sh

REPO="ArsuMinSo/AP4AF"

if [ -z "$GITHUB_TOKEN" ]; then
  echo "Using 'gh' CLI..."
  CREATE_CMD="gh issue create --repo $REPO"
else
  echo "Using GitHub API with token..."
  CREATE_CMD=""
fi

create_issue() {
  local title="$1"
  local body="$2"
  local labels="$3"
  local milestone="$4"

  if [ -n "$GITHUB_TOKEN" ]; then
    curl -s -X POST \
      -H "Authorization: token $GITHUB_TOKEN" \
      -H "Accept: application/vnd.github+json" \
      -H "X-GitHub-Api-Version: 2022-11-28" \
      "https://api.github.com/repos/$REPO/issues" \
      -d "$(jq -n \
        --arg title "$title" \
        --arg body "$body" \
        --argjson labels "$(echo "$labels" | jq -R 'split(",") | map(ltrimstr(" ") | rtrimstr(" "))')" \
        '{title: $title, body: $body, labels: $labels}')"
    echo "Created: $title"
  else
    gh issue create --repo "$REPO" \
      --title "$title" \
      --body "$body" \
      --label "$labels"
    echo "Created: $title"
  fi
  sleep 1
}

# First, create labels
create_labels() {
  local labels=(
    "setup:0075ca:Project setup and scaffolding"
    "backend:e4e669:Backend implementation"
    "database:0052cc:Database related"
    "webapi:bfd4f2:WebAPI endpoints"
    "aspire:d93f0b:Aspire integration"
    "infrastructure:c2e0c6:Infrastructure setup"
    "testing:0e8a16:Tests and testing"
    "frontend:f9d0c4:Frontend/UI implementation"
    "blazor:e99695:Blazor specific"
    "feature:a2eeef:New feature"
    "security:b60205:Security related"
    "documentation:0075ca:Documentation"
    "bonus:fef2c0:Bonus task"
  )

  for label_def in "${labels[@]}"; do
    IFS=':' read -r name color description <<< "$label_def"
    if [ -n "$GITHUB_TOKEN" ]; then
      curl -s -X POST \
        -H "Authorization: token $GITHUB_TOKEN" \
        -H "Accept: application/vnd.github+json" \
        "https://api.github.com/repos/$REPO/labels" \
        -d "{\"name\": \"$name\", \"color\": \"$color\", \"description\": \"$description\"}" > /dev/null
    else
      gh label create "$name" --color "$color" --description "$description" --repo "$REPO" 2>/dev/null || true
    fi
    echo "Label: $name"
  done
}

echo "Creating labels..."
create_labels

echo ""
echo "Creating issues..."

create_issue \
  "Setup: Solution structure and project scaffolding" \
  "## Description
Create the .NET 10 solution with all required projects, correct naming, and project references.

## Projects to create
- \`UTB.Minute.AppHost\` – .NET Aspire AppHost
- \`UTB.Minute.Db\` – Entities and DbContext
- \`UTB.Minute.DbManager\` – WebAPI for database HTTP commands (references \`UTB.Minute.Db\`)
- \`UTB.Minute.Contracts\` – DTOs (Data Transfer Objects)
- \`UTB.Minute.WebApi\` – Shared WebAPI for all clients incl. SSE (references \`UTB.Minute.Db\`, \`UTB.Minute.Contracts\`)
- \`UTB.Minute.WebApi.Tests\` – WebAPI tests using real PostgreSQL (references \`UTB.Minute.WebApi\`)
- \`UTB.Minute.AdminClient\` – Blazor Server for canteen management (references \`UTB.Minute.Contracts\`)
- \`UTB.Minute.CanteenClient\` – Blazor Server for students and cooks (references \`UTB.Minute.Contracts\`)

## Acceptance Criteria
- [ ] All projects exist and are correctly named
- [ ] Project references match the specification
- [ ] Solution builds without errors or warnings
- [ ] Source code language is English
- [ ] .NET 10 is used" \
  "setup,backend" \
  "Mid-semester submission"

create_issue \
  "Backend: Data model and Entity Framework setup" \
  "## Description
Design and implement the data model using Entity Framework Core with PostgreSQL.

## Entities to create
- \`Food\` – id, name, description, price, is_active
- \`MenuItem\` – id, date, food_id (FK), available_portions
- \`Order\` – id, menu_item_id (FK), created_at, status (enum)

## Order status enum
- \`Preparing\` – portions count reduced
- \`Ready\` – ready for pickup
- \`Cancelled\` – order cancelled (portions NOT returned)
- \`Completed\` – picked up / student notified of cancellation

## Acceptance Criteria
- [ ] All entities defined in \`UTB.Minute.Db\` project
- [ ] \`AppDbContext\` correctly configured with all entities and relationships
- [ ] Order status implemented as an enum
- [ ] EF Core migrations created and working with PostgreSQL
- [ ] Database can be created via Aspire" \
  "backend,database" \
  "Mid-semester submission"

create_issue \
  "Backend: DTOs and Contracts layer" \
  "## Description
Create Data Transfer Objects (DTOs) in the \`UTB.Minute.Contracts\` project. DTOs must be independent of entities and defined only once (no duplication).

## DTOs to create
- \`FoodDto\`, \`CreateFoodDto\`, \`UpdateFoodDto\`
- \`MenuItemDto\`, \`CreateMenuItemDto\`, \`UpdateMenuItemDto\`
- \`OrderDto\`, \`CreateOrderDto\`, \`UpdateOrderStatusDto\`

## Acceptance Criteria
- [ ] All DTOs defined only in \`UTB.Minute.Contracts\`
- [ ] DTOs are independent of EF entities
- [ ] No entity types exposed directly through WebAPI
- [ ] Code is not duplicated across projects" \
  "backend" \
  "Mid-semester submission"

create_issue \
  "Backend: Food management WebAPI endpoints" \
  "## Description
Implement Minimal WebAPI endpoints for food management in \`UTB.Minute.WebApi\`.

## Endpoints
- \`GET /foods\` – list all foods
- \`GET /foods/{id}\` – get food by id
- \`POST /foods\` – create new food
- \`PUT /foods/{id}\` – update food
- \`PATCH /foods/{id}/deactivate\` – deactivate food (no hard delete)

## Acceptance Criteria
- [ ] All endpoints implemented using Minimal WebAPI with TypedResults
- [ ] Returns DTOs, not entities
- [ ] Food deactivation sets \`IsActive = false\` (no hard delete)
- [ ] Endpoints have corresponding tests in \`UTB.Minute.WebApi.Tests\`" \
  "backend,webapi" \
  "Mid-semester submission"

create_issue \
  "Backend: Menu management WebAPI endpoints" \
  "## Description
Implement Minimal WebAPI endpoints for daily menu management in \`UTB.Minute.WebApi\`.

## Endpoints
- \`GET /menu\` – list all menu items (date, food, available portions)
- \`GET /menu/{id}\` – get menu item by id
- \`GET /menu/today\` – get today's menu items
- \`POST /menu\` – create menu item
- \`PUT /menu/{id}\` – update menu item
- \`DELETE /menu/{id}\` – delete menu item

## Acceptance Criteria
- [ ] All endpoints implemented using Minimal WebAPI with TypedResults
- [ ] Returns DTOs, not entities
- [ ] Endpoints have corresponding tests in \`UTB.Minute.WebApi.Tests\`" \
  "backend,webapi" \
  "Mid-semester submission"

create_issue \
  "Backend: Order management WebAPI endpoints" \
  "## Description
Implement Minimal WebAPI endpoints for order management in \`UTB.Minute.WebApi\`.

## Endpoints
- \`GET /orders\` – list all non-completed orders (for cooks)
- \`GET /orders/{id}\` – get order by id
- \`POST /orders\` – create new order (reduces available portions)
- \`PATCH /orders/{id}/status\` – update order status

## Business Rules
- Creating an order reduces \`available_portions\` on the menu item
- Cancelling an order does NOT restore portions
- Invalid status transitions must be rejected

## Valid status transitions
- \`Preparing\` → \`Ready\`
- \`Preparing\` → \`Cancelled\`
- \`Ready\` → \`Completed\`
- \`Cancelled\` → \`Completed\` (notification sent)

## Acceptance Criteria
- [ ] All endpoints implemented using Minimal WebAPI with TypedResults
- [ ] Returns DTOs, not entities
- [ ] Invalid status transitions return 400/409 error responses
- [ ] Endpoints have corresponding tests in \`UTB.Minute.WebApi.Tests\`" \
  "backend,webapi" \
  "Mid-semester submission"

create_issue \
  "Backend: Database manager and HTTP reset command" \
  "## Description
Implement the \`UTB.Minute.DbManager\` WebAPI project with HTTP commands for database lifecycle management.

## Endpoints
- \`POST /db/reset\` – drop, recreate, and seed the database with test data

## Seed data should include
- Several sample foods
- Menu items for today and nearby dates
- Sample orders in various states

## Acceptance Criteria
- [ ] \`DbManager\` project exists and references \`UTB.Minute.Db\`
- [ ] Reset endpoint drops and recreates the database
- [ ] Seed data populates the database with realistic test data
- [ ] Endpoint is accessible as HTTP command in Aspire dashboard" \
  "backend,aspire" \
  "Mid-semester submission"

create_issue \
  "Aspire: Infrastructure setup and service discovery" \
  "## Description
Configure .NET Aspire in \`UTB.Minute.AppHost\` to orchestrate all services with proper service discovery.

## Requirements
- PostgreSQL database provisioned via Aspire
- All services registered with Aspire service discovery
- No hardcoded IP addresses or connection strings
- HTTP command configured for database reset

## Acceptance Criteria
- [ ] PostgreSQL database created and configured via Aspire
- [ ] All projects registered in AppHost
- [ ] Service discovery works between all services (no hardcoded IPs)
- [ ] Database reset HTTP command accessible from Aspire dashboard
- [ ] Instructor can run full project with VS 2026, .NET 10, and Docker" \
  "aspire,infrastructure" \
  "Mid-semester submission"

create_issue \
  "Testing: WebAPI integration tests with PostgreSQL" \
  "## Description
Implement automated integration tests for all WebAPI endpoints in \`UTB.Minute.WebApi.Tests\` using a real PostgreSQL database.

## Test coverage required
- Food endpoints: create, read, update, deactivate
- Menu endpoints: create, read, update, delete
- Order endpoints: create, read, status transitions
- Invalid status transition rejection
- Insufficient portions rejection

## Acceptance Criteria
- [ ] Tests use real PostgreSQL (not InMemory EF)
- [ ] Tests run automatically without manual intervention
- [ ] Tests use database provisioned via Aspire
- [ ] All CRUD operations tested
- [ ] Invalid business rule violations tested" \
  "testing" \
  "Mid-semester submission"

create_issue \
  "Frontend: Admin client – Blazor Server for canteen management" \
  "## Description
Implement the \`UTB.Minute.AdminClient\` Blazor Server application for canteen management. Must call WebAPI via HTTP only, no direct database access.

## Food management pages
- List of foods (name, description, price, active status)
- Create new food form
- Edit food form
- Deactivate food button

## Menu management pages
- List of menu items (date, food, available portions) for all days
- Create menu item form
- Edit menu item form
- Delete menu item button

## Acceptance Criteria
- [ ] AdminClient is a Blazor Server application
- [ ] Calls \`UTB.Minute.WebApi\` via HTTP (no direct database access)
- [ ] References only \`UTB.Minute.Contracts\` (not \`UTB.Minute.Db\`)
- [ ] All CRUD operations for foods and menu items work
- [ ] Application accessible via Aspire dashboard" \
  "frontend,blazor" \
  "Semester submission"

create_issue \
  "Frontend: Student ordering interface in CanteenClient" \
  "## Description
Implement the student-facing pages in \`UTB.Minute.CanteenClient\` for viewing today's menu and placing orders.

## Pages
- Today's menu – list of menu items with food name, description, price, available portions
  - Sold-out items visually distinguished (greyed out, badge, etc.)
- Order button – places order for selected food
- My orders – list of current orders with real-time status

## Acceptance Criteria
- [ ] Students can see today's menu
- [ ] Sold-out items are visually distinct from available items
- [ ] Ordering a food reduces available portions
- [ ] Students can see their order status in real time (via SSE)
- [ ] Calls WebAPI via HTTP only" \
  "frontend,blazor" \
  "Semester submission"

create_issue \
  "Frontend: Cook order management interface in CanteenClient" \
  "## Description
Implement the cook-facing pages in \`UTB.Minute.CanteenClient\` for managing orders. Access must be secured (Keycloak cook role).

## Pages
- Active orders list – shows all non-completed orders with real-time updates
- Order detail – shows food name and creation time
- Status change buttons – mark order as Ready, Cancelled, or Completed

## Business Rules
- Invalid status transitions must be blocked in the UI
- Cannot change status from \`Cancelled\` to \`Ready\` etc.

## Acceptance Criteria
- [ ] Cooks see all active (non-completed) orders
- [ ] Cooks can change order status (Ready / Cancelled / Completed)
- [ ] Invalid transitions are blocked (UI and API level)
- [ ] Order list updates in real time (via SSE)
- [ ] Accessible only to users with cook role" \
  "frontend,blazor" \
  "Semester submission"

create_issue \
  "Feature: Server-Sent Events (SSE) for real-time order notifications" \
  "## Description
Implement Server-Sent Events in \`UTB.Minute.WebApi\` to push real-time order status updates to all connected clients without authentication.

## SSE Endpoint
- \`GET /orders/events\` – SSE stream broadcasting all order status changes

## Events to broadcast
- Order created
- Order status changed (Preparing → Ready / Cancelled / Completed)

## Client integration
- Student UI auto-refreshes order status when SSE event received
- Cook UI auto-refreshes order list when SSE event received

## Acceptance Criteria
- [ ] SSE endpoint implemented in WebApi
- [ ] Events broadcast to all connected clients without authentication
- [ ] Student UI updates automatically on order status change
- [ ] Cook UI updates automatically when new orders arrive or statuses change" \
  "backend,frontend,feature" \
  "Semester submission"

create_issue \
  "Security: Keycloak authentication and role-based authorization" \
  "## Description
Integrate Keycloak via Aspire for authentication and role-based access control.

## Roles
- \`admin\` – canteen management (AdminClient)
- \`cook\` – order management (CanteenClient cook view)
- \`student\` – ordering (CanteenClient student view)

## Requirements
- Keycloak provisioned and configured via Aspire (no manual setup)
- Backend endpoints secured by role
- UI adapts to logged-in user's role

## Acceptance Criteria
- [ ] Keycloak running via Aspire (no manual setup required)
- [ ] WebAPI endpoints protected based on role
- [ ] AdminClient accessible only to \`admin\` role
- [ ] Cook pages accessible only to \`cook\` role
- [ ] Student ordering accessible to \`student\` role
- [ ] UI shows/hides functionality based on role
- [ ] Instructor can run full project locally (VS 2026, .NET 10, Docker)" \
  "security,aspire" \
  "Semester submission"

create_issue \
  "Feature: Concurrency handling for last available portion" \
  "## Description
Implement database-level concurrency control to handle race conditions when multiple students order the last available portion simultaneously.

## Implementation options
- Optimistic concurrency with \`RowVersion\` / ETag on \`MenuItem\`
- Pessimistic locking via database transaction
- Application-level lock with retry logic

## Acceptance Criteria
- [ ] Two simultaneous orders for the last portion result in only one success
- [ ] Second request receives appropriate error response (e.g., 409 Conflict)
- [ ] Implementation uses database-level or transaction-level concurrency
- [ ] Test demonstrates correct behavior under concurrent access" \
  "backend,feature" \
  "Semester submission"

create_issue \
  "Documentation: README.md" \
  "## Description
Write comprehensive project documentation in \`README.md\` following the provided template.

## Required sections
- Project overview and architecture decisions
- How to run the project locally (prerequisites: VS 2026, .NET 10, Docker)
- Project structure explanation
- Known issues or problems encountered
- Team member contribution ratio (e.g., \`1:1:1\` for equal contributions)

## Acceptance Criteria
- [ ] README.md exists and is written in Markdown
- [ ] Covers all required sections
- [ ] Architecture decisions explained
- [ ] Prerequisites and run instructions clear
- [ ] Team contribution ratio included" \
  "documentation" \
  "Mid-semester submission"

create_issue \
  "Bonus: Avalonia desktop client for cooks" \
  "## Description
Implement a desktop application for cooks using the Avalonia framework as a bonus task.

## Requirements
- Displays active orders
- Allows status changes (Ready / Cancelled / Completed)
- Uses the same WebAPI as other clients
- Real-time updates via SSE

## Acceptance Criteria
- [ ] Avalonia desktop app builds and runs on Windows
- [ ] Connects to \`UTB.Minute.WebApi\` via HTTP
- [ ] All cook functionality available
- [ ] Real-time updates working via SSE" \
  "bonus,frontend" \
  "Optional"

echo ""
echo "All issues created!"
