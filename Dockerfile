# Use the .NET 10 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022 AS build

WORKDIR /src

# Copy all the source code
COPY . .

# Restore and publish the application as a self-contained executable
RUN dotnet publish -c Release -o /app/publish --runtime win-x64 --self-contained true

# Final image
FROM mcr.microsoft.com/dotnet/runtime:10.0-windowsservercore-ltsc2022 AS final

WORKDIR /app

# Copy the built application
COPY --from=build /app/publish .
# Copy the test data
COPY test-data/ .
# Copy the source for the dotfile
COPY src/ .

ENTRYPOINT ["powershell", "-File", "run-test.ps1"]