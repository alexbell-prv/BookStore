#!/usr/bin/env bash
set -Eeuo pipefail

project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$project_root"

if [[ ! -f .env ]]; then
    printf 'Missing .env. Copy .env.example to .env and set the SQL Server credentials.\n' >&2
    exit 1
fi

set -a
source .env
set +a

: "${MSSQL_SA_PASSWORD:?Set MSSQL_SA_PASSWORD in .env}"
: "${MANAGEMENT_CLIENT_SECRET:?Set MANAGEMENT_CLIENT_SECRET in .env}"

export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Identity="Server=localhost,1433;Database=BookStore.Identity;User ID=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True"
export OAuth__ManagementClientSecret="${MANAGEMENT_CLIENT_SECRET}"

exec dotnet run --project "$project_root/BookStore.IdentityServer" --launch-profile http
