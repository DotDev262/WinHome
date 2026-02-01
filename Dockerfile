# Use the .NET 10 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022 AS build

WORKDIR /src

# Copy all the source code
COPY . .

# Restore and publish the application as a self-contained executable
RUN dotnet publish src/WinHome.csproj -c Release -o /app/publish --runtime win-x64 --self-contained true

# Final image - using a fuller Windows image for better compatibility with package managers
FROM mcr.microsoft.com/windows/server:ltsc2022 AS final

WORKDIR /app

# Set execution policy for the container
RUN powershell -Command "Set-ExecutionPolicy Bypass -Scope LocalMachine -Force"

# Copy the built application
COPY --from=build /app/publish .
# Copy the test data
COPY test-data/ .
# Copy the source for the dotfile
COPY src/ .
# Copy README.md for the dotfile test
COPY README.md .

USER ContainerAdministrator

ENTRYPOINT ["powershell", "-File", "run-test-container.ps1"]