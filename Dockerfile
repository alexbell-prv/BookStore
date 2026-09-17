FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY Directory.Build.props global.json BookStore.slnx ./
COPY BookStore.Models/BookStore.Models.csproj BookStore.Models/
COPY BookStore.Infrastructure/BookStore.Infrastructure.csproj BookStore.Infrastructure/
COPY BookStore.Repository/BookStore.Repository.csproj BookStore.Repository/
COPY BookStore.BusinessLogic/BookStore.BusinessLogic.csproj BookStore.BusinessLogic/
COPY BookStore.API/BookStore.API.csproj BookStore.API/
COPY BookStore.Host/BookStore.Host.csproj BookStore.Host/
COPY BookStore.IdentityServer/BookStore.IdentityServer.csproj BookStore.IdentityServer/
COPY BookStore.Tests/BookStore.Tests.csproj BookStore.Tests/
RUN dotnet restore BookStore.slnx
COPY . .

FROM build AS publish-api
RUN dotnet publish BookStore.Host/BookStore.Host.csproj -c Release --no-restore -o /out

FROM build AS publish-identity
RUN dotnet publish BookStore.IdentityServer/BookStore.IdentityServer.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS runtime
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
RUN mkdir -p /app/keys && chown -R $APP_UID:$APP_UID /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=6 CMD curl --fail --silent http://localhost:8080/health/ready || exit 1

FROM runtime AS api
COPY --from=publish-api /out .
ENTRYPOINT ["dotnet", "BookStore.Host.dll"]

FROM runtime AS identityserver
COPY --from=publish-identity /out .
ENTRYPOINT ["dotnet", "BookStore.IdentityServer.dll"]

