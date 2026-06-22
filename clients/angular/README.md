# iM.Cloud Angular API client

TypeScript client generated from the backend OpenAPI document via [NSwag](https://github.com/RicoSuter/NSwag).

**Do not edit generated files by hand.** After backend API changes, regenerate with the script below.

Generated artifacts in this folder:

- `im-cloud-api-client.ts` — Angular `HttpClient` services and DTOs (NSwag output)
- `api-client.extension.ts` — merged into the client on regenerate (keep minimal; no duplicate exports)

## Regenerate

From the repository root:

```bash
./scripts/generate-api-client.sh
```

Or manually:

```bash
dotnet build src/iM.Cloud.API/iM.Cloud.API.csproj -c Debug
dotnet msbuild src/iM.Cloud.API/iM.Cloud.API.csproj -p:GenerateApiClient=true
```

Outputs:

- `openapi/iM.Cloud.openapi.json` — OpenAPI 3 spec
- `clients/angular/im-cloud-api-client.ts` — Angular client

## Use in `iM.Cloud.Client`

The Angular app depends on this package via npm (`package.json` → `"@im-cloud/api": "file:../../clients/angular"`). After regenerating, no copy step is required.

```typescript
import { API_BASE_URL, AuthClient, AdminGroupsClient } from '@im-cloud/api';
```

Provide `API_BASE_URL` and `HttpClient` in `app.config.ts`. Wrap generated clients in app services when you need extra logic — never modify `im-cloud-api-client.ts` directly.
