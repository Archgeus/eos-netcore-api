# EOS API - .NET Core 8.0

Complete .NET Core 8.0 rewrite of the PHP EOS API for the EOS game platform. Full API compatibility with hardcoded game client routes.

## Installation

### Prerequisites

1. **.NET 8.0 SDK** (Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0))
   - Windows, macOS, or Linux
   - Verify: `dotnet --version` should show 8.0.x

2. **MySQL Server** (5.7+)
   - Download from [mysql.com](https://dev.mysql.com/downloads/mysql/)
   - Create database: `admin_eos_api`
   - Create user: `admin_eos_api` with appropriate permissions

3. **Git** (optional, for cloning)

### Step 1: Clone and Navigate

```bash
git clone https://github.com/Archgeus/eos-netcore-api/
cd eos-netcore-api
```

### Step 2: Configure Database Connection

Edit `appsettings.json` with your MySQL credentials:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=127.0.0.1;Database=admin_eos_api;User=admin_eos_api;Password=YOUR_PASSWORD"
}
```

Also update Server settings if needed:

```json
"Server": {
  "Host": "0.0.0.0",    // Listen address (0.0.0.0 = all interfaces)
  "Port": "80"          // Listen port
}
```

### Step 3: Install Dependencies

```bash
dotnet restore
```

### Step 4: Build Project

```bash
dotnet build
```

### Step 5: Run API

```bash
dotnet run
```

The API will:
1. **Apply migrations automatically** (creates tables if they don't exist)
2. **Test database connection** and display connected database name
3. **Start listening** on configured host:port
4. **Display** user count and confirmation message

Expected console output:

```
✓ DATABASE CONNECTED SUCCESSFULLY
  Database: admin_eos_api
  Users: 7 accounts

info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:80
```

### Step 6: Verify Installation

**Swagger Documentation** (interactive API testing):
- Open browser: `http://localhost:80/swagger`
- "Try it out" on any endpoint to test

**Test Login Endpoint** (curl):
```bash
curl -X POST http://localhost:80/login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=archgeus&password=password"
```

Expected response: 64-character token

---

## Quick Start (After Installation)

```bash
cd eos-netcore-api
dotnet run
```

Server listens on `http://0.0.0.0:80` (configurable via `appsettings.json`)

---

## Configuration

All settings in `appsettings.json`:

| Setting | Purpose | Default |
|---------|---------|---------|
| `Server:Host` | Listen address | `0.0.0.0` |
| `Server:Port` | Listen port | `80` |
| `AppSettings:TokenExpiryMinutes` | Token TTL | `20` |
| `AppSettings:AccountSalt` | Password salt | `@#56qtasfGSAG` |

---

## Database Schema

### Users Table
```sql
CREATE TABLE Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(255) UNIQUE NOT NULL,
    Password LONGTEXT NOT NULL,
    Balance INT NOT NULL DEFAULT 0,
    JoinDate DATETIME(6) NOT NULL,
    LastLogin DATETIME(6) NULL
) CHARACTER SET utf8mb4;
```

### Tokens Table
```sql
CREATE TABLE Tokens (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    TokenValue VARCHAR(64) NOT NULL,
    Username LONGTEXT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_Tokens_UserId (UserId)
) CHARACTER SET utf8mb4;
```

### PurchaseTransactions Table
```sql
CREATE TABLE PurchaseTransactions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    RequestId VARCHAR(255) NOT NULL,
    Price INT NOT NULL,
    TransactionId INT NOT NULL,
    ItemId INT NOT NULL,
    ItemName LONGTEXT NOT NULL,
    UserIp VARCHAR(45) NULL,
    CreatedAt DATETIME(6) NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_PurchaseTransactions_UserId (UserId)
) CHARACTER SET utf8mb4;
```

### FundsTransactions Table
```sql
CREATE TABLE FundsTransactions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    TransactionId VARCHAR(255) NOT NULL,
    Amount INT NOT NULL,
    Operation VARCHAR(50) NOT NULL,          -- "add" or "remove"
    BalanceBefore INT NOT NULL,
    BalanceAfter INT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_FundsTransactions_UserId (UserId)
) CHARACTER SET utf8mb4;
```

---

## API Endpoints

### Authentication

#### Register User
```
POST /register
Content-Type: application/x-www-form-urlencoded

username=testuser&password=MyPassword123
```

Response:
```json
{
  "message": "Registration successful"
}
```

#### Login
```
POST /login
Content-Type: application/x-www-form-urlencoded

username=testuser&password=MyPassword123
```

Response: Plain text token (64 chars, alphanumeric)
```
MsviVh5Ayipyw2UApUG9vDAL3ZStEqhHxJyLHKEFYHUSQcUJwUG268oP6JAVWxjX
```

#### OAuth Token Exchange
```
POST /oauth/access_token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&code=TOKEN&client_id=xxx&client_secret=xxx&scope=scope_general,scope_billing
```

Response:
```json
{
  "access_token": "TOKEN",
  "expires_in": 1200,
  "token_type": "bearer",
  "scope": null,
  "refresh_token": "TOKEN"
}
```

#### Refresh Token (GET)
```
GET /oauth/access_token?refresh_token=TOKEN
```

**Purpose**: When in-game, if the access token expires (>20 minutes), the client can use this endpoint to get a fresh access token without re-entering credentials.

**How it works**:
- The `refresh_token` returned from login/OAuth is reusable (doesn't expire)
- Pass it as a query parameter to get a new `access_token` with a fresh 20-minute expiry
- New token starts with CreatedAt = now, resetting the countdown

Response: Returns new access_token and refresh_token
```json
{
  "access_token": "NEW_TOKEN_VALUE",
  "expires_in": 1200,
  "token_type": "bearer",
  "scope": null,
  "refresh_token": "NEW_REFRESH_TOKEN"
}
```

**Example flow**:
1. Login: POST /login → get `token` and `refresh_token`
2. Play game (20+ minutes) → token expires
3. Before next API call fails with 401: GET /oauth/access_token?refresh_token=REFRESH → get fresh `token`
4. Resume API calls with new token

---

### User Service Endpoints (Game Client)

All user endpoints require `access_token` in query parameters. All return `data` + `header` wrapper with `request_id` and Unix timestamp `elapsed_time`.

#### Get User Info
```
GET /services/v2/user/me?fields=uid,username&access_token=TOKEN
```

Response:
```json
{
  "data": {
    "uid": "7",
    "username": "testuser"
  },
  "header": {
    "request_id": "0HNL0PG7V5PK7:00000001",
    "elapsed_time": 1776901564
  }
}
```

#### Get Account Balance
```
GET /services/v2/user/mi/ap?access_token=TOKEN
```

Response:
```json
{
  "data": {
    "balance": "995345"
  },
  "header": {
    "request_id": "0HNL0PG7V5PK7:00000001",
    "elapsed_time": 1776902905
  }
}
```

#### Make Purchase
```
POST /services/v2/user/mi/item_purchase?access_token=TOKEN
Content-Type: application/x-www-form-urlencoded

price=11326&txn_id=123&item_id=42&item_name=Dragon%20Sword&user_ip=127.0.0.1
```

Response:
```json
{
  "data": {
    "balance": "984019",
    "txn_detail": 0
  },
  "header": {
    "request_id": "0HNL0PG7V5PK7:00000001",
    "elapsed_time": 1776902905
  }
}
```

Logged to `PurchaseTransactions` table with RequestId, Price, TransactionId, ItemId, ItemName, UserIp, CreatedAt.

---

### Admin Endpoints

#### Get User by ID
```
GET /GetUserInfoById?id=7
```

Response:
```json
{
  "id": 7,
  "username": "testuser",
  "join_date": "2026-04-23T10:15:30",
  "last_login": "2026-04-23T15:45:20"
}
```

#### List All Accounts
```
GET /ListAccounts
```

Response:
```json
[
  {
    "id": 1,
    "username": "admin",
    "join_date": "2026-04-20T08:00:00",
    "last_login": "2026-04-23T14:30:00"
  },
  {
    "id": 7,
    "username": "testuser",
    "join_date": "2026-04-23T10:15:30",
    "last_login": "2026-04-23T15:45:20"
  }
]
```

#### List Online Accounts
Returns users with valid, non-expired tokens (created within last 20 minutes).

```
GET /ListOnlineAccounts
```

Response:
```json
[
  {
    "id": 7,
    "username": "testuser",
    "join_date": "2026-04-23T10:15:30",
    "last_login": "2026-04-23T15:45:20"
  }
]
```

#### Add Funds to Account
```
POST /AddFundsByUserId
Content-Type: application/x-www-form-urlencoded

id=7&tnx_id=999&amount=50000
```

Response:
```json
{
  "message": "Funds added successfully",
  "new_balance": 1034019
}
```

Logs: `[ADD_FUNDS] UserID=7, Amount=50000, TnxID=999, BalanceBefore=984019, BalanceAfter=1034019`

Records transaction in FundsTransactions table: `Operation="add", Amount=50000, BalanceBefore=984019, BalanceAfter=1034019`

#### Remove Funds from Account
```
POST /RemFundsByUserId
Content-Type: application/x-www-form-urlencoded

id=7&tnx_id=998&amount=10000
```

Response:
```json
{
  "message": "Funds removed successfully",
  "new_balance": 1024019
}
```

Logs: `[REM_FUNDS] UserID=7, Amount=10000, TnxID=998, BalanceBefore=1034019, BalanceAfter=1024019`

Records transaction in FundsTransactions table: `Operation="remove", Amount=10000, BalanceBefore=1034019, BalanceAfter=1024019`

Logs: `[REM_FUNDS] UserID=7, Amount=10000, TnxID=998, NewBalance=1024019`

Error if insufficient balance:
```json
{
  "error": "Insufficient balance"
}
```

---

## Database Connection Management

### Connection Pool & Health Checks

The API maintains database connection health with automatic management:

- **Connection Pooling**: Pomelo automatically manages connection pool (min 5, auto-scale)
- **Health Checks**: `DatabaseHealthCheckService` pings database every 5 minutes
- **Reconnection**: Auto-reconnects on transient failures
- **Idle Timeout Prevention**: Regular pings prevent MySQL from closing idle connections

Console logs (Development mode):
```
info: EOS.Services.DatabaseHealthCheckService[0]
      Database Health Check Service started. Pinging every 300 seconds.
dbg: EOS.Services.DatabaseHealthCheckService[0]
      Database ping successful at 2026-04-23T12:30:45.1234567Z
```

No configuration needed - works automatically on startup.

---

## Security

- **Passwords**: SHA512 hashed with custom salt (`@#56qtasfGSAG`)
- **Tokens**: 64-character alphanumeric, 20-minute expiry
- **Token Refresh**: Expired tokens can be refreshed using the `refresh_token` endpoint without re-login:
  - Access tokens expire after 20 minutes (configurable in appsettings.json)
  - Game client can refresh via: `GET /oauth/access_token?refresh_token=TOKEN`
  - New token has fresh 20-minute expiry from moment of refresh
  - Allows seamless long gaming sessions without interruption
- **Token Validation**: Automatic cleanup of expired tokens, validation checks CreatedAt within expiry window
- **Response Logging**: All request/response bodies logged in debug mode

---

## Debugging

### Response Logging

The `ResponseLoggingMiddleware` logs all request/response bodies to console in real-time:

```
[PURCHASE REQUEST] item_id=42, item_name=Dragon Sword, price=11326, txn_id=123, user_ip=127.0.0.1
[PURCHASE SAVED] TransactionID=1, ItemID=42, ItemName=Dragon Sword, Price=11326
info: EOS.Middleware.ResponseLoggingMiddleware[0]
      Response Body: {"data":{"balance":"984019","txn_detail":0},"header":{...}}
```

### Swagger/OpenAPI

Documentation available at: `http://localhost/swagger`

Try out endpoints interactively with "Try it out" button.

---

## Troubleshooting

### .NET SDK Not Found
**Error:** `dotnet: command not found` or `'dotnet' is not recognized`

**Solution:** 
1. Download .NET 8.0 from https://dotnet.microsoft.com/download/dotnet/8.0
2. Install and verify: `dotnet --version`
3. Restart terminal/IDE

### Migrations Failed
**Error:** `Unable to apply pending migrations`

**Solution:**
- Clear and rebuild: 
  ```bash
  dotnet clean
  dotnet restore
  dotnet build
  ```

---

## Build & Deploy

### Development
```bash
dotnet run
```

### Release Build
```bash
dotnet publish -c Release
```

### Run Release Build
```bash
cd bin\Release
EOS.exe
```

---

## Deployment Configuration

For game client hardcoded addresses, configure in `appsettings.json`:

```json
"Server": {
  "Host": "127.0.0.1",
  "Port": 80
}
```

API will restart and listen on `http://127.0.0.1:80`

---

## Architecture

- **Controllers**: API endpoints (Auth, User, Admin)
- **Services**: Business logic (Auth, User, Token management)
- **Models**: EF Core entities (User, Token, PurchaseTransaction)
- **Data**: DbContext and migrations
- **Middleware**: Response logging for debugging

---

## Technology Stack

- Framework: ASP.NET Core 8.0
- Database: MySQL 8.0 (Pomelo.EntityFrameworkCore.MySql)
- ORM: Entity Framework Core 8.0
- Password Hashing: SHA512 (PHP-compatible)
- Documentation: Swagger/OpenAPI 3.0

---

## Notes

- Token expiry: 20 minutes (configurable)
- Purchase tracking: All transactions logged with item details
- Online status: Based on valid token existence
- Last login: Updated on every successful authentication
