#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_PROJECT="$ROOT/src/iM.Cloud.API/iM.Cloud.API.csproj"

echo "Building API..."
dotnet build "$API_PROJECT" -c Debug --no-restore 2>/dev/null || dotnet build "$API_PROJECT" -c Debug

echo "Generating OpenAPI spec and Angular TypeScript client..."
dotnet msbuild "$API_PROJECT" -p:GenerateApiClient=true -v:minimal

echo "Done."
echo "  OpenAPI:  openapi/iM.Cloud.openapi.json"
echo "  Client:   clients/angular/im-cloud-api-client.ts"
