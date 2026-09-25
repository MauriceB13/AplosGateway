# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AplosGateway.sln", "./"]
COPY ["src/AplosGateway.Api/AplosGateway.Api.csproj", "src/AplosGateway.Api/"]
COPY ["src/AplosGateway.Core/AplosGateway.Core.csproj", "src/AplosGateway.Core/"]
COPY ["src/AplosGateway.Infrastructure/AplosGateway.Infrastructure.csproj", "src/AplosGateway.Infrastructure/"]
COPY ["tests/AplosGateway.Tests/AplosGateway.Tests.csproj", "tests/AplosGateway.Tests/"]

RUN dotnet restore "AplosGateway.sln"

COPY . .

RUN dotnet publish "src/AplosGateway.Api/AplosGateway.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

RUN mkdir -p /app/Data \
    && chown -R app:app /app/Data

EXPOSE 8080

VOLUME ["/app/Data"]

USER app

ENTRYPOINT ["dotnet", "AplosGateway.Api.dll"]
