# Use the official .NET 6 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5000

# Use the .NET 6 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ProductCatalogApp.csproj", "."]
RUN dotnet restore "ProductCatalogApp.csproj"
COPY . .
WORKDIR "/src"

# Build configuration based on arg
ARG ASPNETCORE_CONFIGURATION=Release
RUN dotnet build "ProductCatalogApp.csproj" -c ${ASPNETCORE_CONFIGURATION} -o /app/build

FROM build AS publish
ARG ASPNETCORE_CONFIGURATION=Release
RUN dotnet publish "ProductCatalogApp.csproj" -c ${ASPNETCORE_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for database and ensure permissions
RUN mkdir -p /app/data /app/logs && \
    chmod 755 /app/data /app/logs

# Performance environment variables for 5000+ products
ENV DOTNET_gcServer=1 \
    DOTNET_gcConcurrent=1 \
    DOTNET_ThreadPool_MinWorkerThreads=16 \
    DOTNET_ThreadPool_MinCompletionPortThreads=16 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "ProductCatalogApp.dll"]
