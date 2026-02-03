# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022 AS build

WORKDIR /src

# Copy only project files first for better layer caching
COPY src/*.csproj src/
COPY tests/WinHome.Tests/*.csproj tests/WinHome.Tests/

# Restore dependencies
RUN dotnet restore src/WinHome.csproj

# Copy remaining source code
COPY src/ src/
COPY tests/ tests/

# Publish as self-contained executable
RUN dotnet publish src/WinHome.csproj \
    -c Release \
    -o /app/publish \
    --runtime win-x64 \
    --self-contained true \
    --no-restore \
    /p:PublishSingleFile=true

# Runtime stage - Server Core for CLI-only tools (faster in CI, ~3-4GB smaller)
FROM mcr.microsoft.com/windows/servercore:ltsc2022 AS final

WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy test resources
COPY test-data/ .
COPY tests/ tests/
COPY src/ src/
COPY README.md .

# Run as admin to allow tool to install dependencies
USER ContainerAdministrator

# Install Pester for testing
RUN powershell -Command "Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force; Install-Module -Name Pester -Force -SkipPublisherCheck"

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD powershell -Command ".\WinHome.exe --help" || exit 1

ENTRYPOINT ["powershell", "-File", "run-test-container.ps1"]