# AplosGateway

AplosGateway is a .NET 8 API that provides a secure integration layer between
Virtuous/Make and the Aplos API.

## Configuration

AplosGateway uses standard ASP.NET Core configuration. Environment variables
override values in `appsettings.json`.

Nested configuration keys use double underscores (`__`) in environment
variable names.

### Required production secrets

The following values must be supplied securely by the deployment environment.
Do not commit their values to source control.

| Environment variable | Purpose |
| --- | --- |
| `Security__ApiKey` | Bearer API key required for protected AplosGateway endpoints |
| `Aplos__ClientId` | Aplos API client identifier |
| `Aplos__PrivateKey` | Base64 PKCS#8 private key used to decrypt Aplos access tokens |

### Transaction posting safety switch

`Aplos__AllowTransactionPosting` controls whether AplosGateway may create
transactions in Aplos.

Production deployments should initially use:

`Aplos__AllowTransactionPosting=false`

Enable transaction posting only after deployment configuration and connectivity
have been verified.

### Non-secret configuration

The following values have defaults in `appsettings.json` and may be overridden
by the deployment environment when necessary:

| Environment variable | Purpose |
| --- | --- |
| `Aplos__BaseUrl` | Aplos API base URL |
| `Virtuous__OrganizationId` | Expected Virtuous organization ID |
| `TransactionMapping__DepositAccountNumber` | Aplos deposit account number |
| `TransactionMapping__IncomeAccountNumber` | Aplos income account number |
| `TransactionMapping__FundId` | Aplos API fund ID |
| `Idempotency__ConnectionString` | SQLite idempotency database connection string |
| `Gateway__Name` | Gateway name reported by the liveness endpoint |
| `Gateway__Version` | Gateway version reported by the liveness endpoint |

## Health endpoints

### `GET /health`

Public liveness endpoint. Confirms that the API process is running.

### `GET /health/ready`

Public readiness endpoint. Confirms that required operational configuration is
present without contacting Aplos or exposing secret values.

A ready gateway returns HTTP `200`.

A gateway missing operational configuration returns HTTP `503`.

A missing `Security__ApiKey` is treated as a fatal security configuration error
rather than a normal readiness failure.

## Runtime data

AplosGateway uses SQLite to persist Virtuous gift idempotency records.

The default connection string is:

`Data Source=Data/aplosgateway.db`

The `Data` directory must use persistent storage in production. Losing this
database can cause the gateway to lose its record of previously processed
Virtuous gifts.

The `Data` directory, `.env` files, and the local `Secrets` directory are
excluded from source control.

## Secret handling

Do not store the gateway API key, Aplos client ID, Aplos private key, decrypted
Aplos access tokens, or other credentials in:

- `appsettings.json`
- `appsettings.Development.json`
- `launchSettings.json`
- `.env` files committed to source control
- application logs

Use the deployment platform's secret-management or environment-variable
facility instead.