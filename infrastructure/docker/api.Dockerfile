FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY apps/api/src/LolAnalyzer.Domain/LolAnalyzer.Domain.csproj apps/api/src/LolAnalyzer.Domain/
COPY apps/api/src/LolAnalyzer.Application/LolAnalyzer.Application.csproj apps/api/src/LolAnalyzer.Application/
COPY apps/api/src/LolAnalyzer.Infrastructure/LolAnalyzer.Infrastructure.csproj apps/api/src/LolAnalyzer.Infrastructure/
COPY apps/api/src/LolAnalyzer.Api/LolAnalyzer.Api.csproj apps/api/src/LolAnalyzer.Api/
RUN dotnet restore apps/api/src/LolAnalyzer.Api/LolAnalyzer.Api.csproj
COPY apps/api/src apps/api/src
RUN dotnet publish apps/api/src/LolAnalyzer.Api/LolAnalyzer.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "LolAnalyzer.Api.dll"]
