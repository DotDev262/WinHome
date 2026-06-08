# Stage 1: Build & Cross-Compile the Windows-Native binary on a Linux host
FROM ://microsoft.com AS build
WORKDIR /src
COPY WinHome.sln .
COPY src/ src/
RUN dotnet restore src/WinHome.csproj
RUN dotnet publish -c Release --no-restore -o /app -r win-x64 --self-contained true

# Stage 2: Artifact Export Layer
# WinHome is fundamentally a Windows-native CLI tool and cannot run on a Linux container kernel.
# The compiled binary (win-x64) must be extracted and executed on native Windows environments.
# This scratch stage serves cleanly as a build artifact container exporter, not a runtime service.
FROM scratch AS artifact
COPY --from=build /app .
