Apologies for the Linux lingo, I use it at home but use Windows at work.

There are 2 bash scripts to start the project on Linux. On Windows it is easier to use Visual Studio.

Startup order:

1. BookStore.IdentityServer
2. BookStore.Host

Swagger is here:

http://localhost:5093/swagger

To start everything with Docker:

docker compose up --build -d

The Docker setup also runs the migrations.

To start it on Linux:

Copy `.env.example` to `.env`.
Set the SQL password and management secret.
Start SQL Server and wait until it is ready: `docker compose up -d --wait sqlserver`.
Run `./start-identity.bash`.
Run `./start-host.bash` in another terminal.

To run migrations manually:

set -a
source .env
set +a
export ConnectionStrings__Identity="Server=localhost,1433;Database=BookStore.Identity;User ID=sa;Password=$"{MSSQL_SA_PASSWORD}";Encrypt=True;TrustServerCertificate=True"
export ConnectionStrings__BookStore="Server=localhost,1433;Database=BookStore;User ID=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True"
export OAuth__ManagementClientSecret="$MANAGEMENT_CLIENT_SECRET"
dotnet run --project BookStore.IdentityServer -- --migrate
dotnet run --project BookStore.Host -- --migrate

Database connection:

Server: localhost,1433
Username: sa
Password: the MSSQL_SA_PASSWORD value from .env
Encrypt: Yes
Trust server certificate: Yes

The databases are `BookStore` and `BookStore.Identity`.

Keep `.env` private.
