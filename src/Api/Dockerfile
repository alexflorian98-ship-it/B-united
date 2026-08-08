# Build context must be the repository root (see docker-compose.yml), because the Api host
# references every BuildingBlocks/Modules project via ProjectReference.

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/ src/

RUN dotnet restore src/Api/BUnited.Api.csproj

RUN dotnet publish src/Api/BUnited.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S bunited && adduser -S bunited -G bunited
USER bunited

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BUnited.Api.dll"]
