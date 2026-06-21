# iM.Cloud Angular API client

TypeScript client generated from the backend OpenAPI document via [NSwag](https://github.com/RicoSuter/NSwag).

## Regenerate (backend team)

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
- `clients/angular/im-cloud-api-client.ts` — Angular `HttpClient` services and DTOs

## Use in Angular (frontend team)

1. Copy `im-cloud-api-client.ts` and `api-client.extension.ts` into your Angular project (e.g. `src/app/api/`).
2. Register `HttpClient` and provide the API base URL:

```typescript
import { API_BASE_URL } from './api/api-client.extension';
import { AdminGroupsClient } from './api/im-cloud-api-client';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(),
    { provide: API_BASE_URL, useValue: 'https://localhost:5001' },
    AdminGroupsClient,
    // ... other generated clients
  ],
});
```

3. Inject clients in components/services:

```typescript
constructor(private groups: AdminGroupsClient) {}

loginAndList() {
  return this.groups.getAll({ first: 0, rows: 10 });
}
```

4. After login, set the JWT on requests (example interceptor):

```typescript
const token = localStorage.getItem('accessToken');
const headers = token ? { Authorization: `Bearer ${token}` } : {};
```

Regenerate the client whenever backend API contracts change.
