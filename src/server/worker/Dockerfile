# ============================================
# Build Stage
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["worker/TicketFlow.Worker.csproj", "worker/"]
COPY ["backend/Application/TicketFlow.Application.csproj", "backend/Application/"]
COPY ["backend/Infrastructure/TicketFlow.Infrastructure.csproj", "backend/Infrastructure/"]
COPY ["backend/Domain/TicketFlow.Domain.csproj", "backend/Domain/"]

# Restore dependencies
RUN dotnet restore "worker/TicketFlow.Worker.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/worker"
RUN dotnet build "TicketFlow.Worker.csproj" -c Release -o /app/build

# ============================================
# Publish Stage
# ============================================
FROM build AS publish
WORKDIR "/src/worker"
RUN dotnet publish "TicketFlow.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# Runtime Stage
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Set environment
ENV DOTNET_ENVIRONMENT=Development

# Run application
ENTRYPOINT ["dotnet", "TicketFlow.Worker.dll"]
