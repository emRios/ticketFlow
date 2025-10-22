# ============================================
# Build Stage
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["Api/TicketFlow.Api.csproj", "Api/"]
COPY ["Application/TicketFlow.Application.csproj", "Application/"]
COPY ["Infrastructure/TicketFlow.Infrastructure.csproj", "Infrastructure/"]
COPY ["Domain/TicketFlow.Domain.csproj", "Domain/"]

# Restore dependencies
RUN dotnet restore "Api/TicketFlow.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/Api"
RUN dotnet build "TicketFlow.Api.csproj" -c Release -o /app/build

# ============================================
# Publish Stage
# ============================================
FROM build AS publish
WORKDIR "/src/Api"
RUN dotnet publish "TicketFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# Runtime Stage
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 5076

# Set environment
ENV ASPNETCORE_URLS=http://+:5076
ENV ASPNETCORE_ENVIRONMENT=Development

# Run application
ENTRYPOINT ["dotnet", "TicketFlow.Api.dll"]
