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

for port in 5093 5094; do
    if ss -H -ltn "sport = :${port}" | grep -q .; then
        printf 'Port %s is already in use. Stop the existing service before starting Bookstore.\n' "$port" >&2
        exit 1
    fi
done

export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Identity="Server=localhost,1433;Database=BookStore.Identity;User ID=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True"
export ConnectionStrings__BookStore="Server=localhost,1433;Database=BookStore;User ID=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True"
export OAuth__ManagementClientSecret="${MANAGEMENT_CLIENT_SECRET}"

identity_pid=""
host_pid=""

cleanup() {
    local status=$?
    trap - EXIT INT TERM
    for process_id in "$host_pid" "$identity_pid"; do
        if [[ -n "$process_id" ]] && kill -0 "$process_id" 2>/dev/null; then
            kill "$process_id" 2>/dev/null || true
        fi
    done
    for process_id in "$host_pid" "$identity_pid"; do
        if [[ -n "$process_id" ]]; then
            wait "$process_id" 2>/dev/null || true
        fi
    done
    exit "$status"
}

wait_for_ready() {
    local name=$1
    local url=$2
    for attempt in $(seq 1 150); do
        if curl --fail --silent --output /dev/null "$url"; then
            return 0
        fi
        sleep 0.2
    done
    printf '%s did not become ready: %s\n' "$name" "$url" >&2
    return 1
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

docker compose up -d --wait --wait-timeout 60 sqlserver
dotnet run --project "$project_root/BookStore.IdentityServer" -- --migrate
dotnet run --project "$project_root/BookStore.Host" -- --migrate

"$project_root/start-identity.bash" &
identity_pid=$!
wait_for_ready 'IdentityServer' 'http://localhost:5094/health/ready'

"$project_root/start-host.bash" &
host_pid=$!
wait_for_ready 'Bookstore Host' 'http://localhost:5093/health/ready'

printf 'Bookstore is ready at http://localhost:5093/swagger\n'
wait -n "$identity_pid" "$host_pid"
