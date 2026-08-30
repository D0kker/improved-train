FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY apps/api/src/LolAnalyzer.Domain/LolAnalyzer.Domain.csproj apps/api/src/LolAnalyzer.Domain/
COPY apps/api/src/LolAnalyzer.Application/LolAnalyzer.Application.csproj apps/api/src/LolAnalyzer.Application/
COPY apps/api/src/LolAnalyzer.Infrastructure/LolAnalyzer.Infrastructure.csproj apps/api/src/LolAnalyzer.Infrastructure/
COPY workers/ingestion-worker/LolAnalyzer.IngestionWorker.csproj workers/ingestion-worker/
RUN dotnet restore workers/ingestion-worker/LolAnalyzer.IngestionWorker.csproj
COPY apps/api/src apps/api/src
COPY workers/ingestion-worker workers/ingestion-worker
RUN dotnet publish workers/ingestion-worker/LolAnalyzer.IngestionWorker.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "LolAnalyzer.IngestionWorker.dll"]
