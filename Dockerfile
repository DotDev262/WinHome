# Stage 1: Build the C# application using the SDK environment
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY WinHome.sln .
COPY src/ src/
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime layer using a slim .NET execution framework
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./WinHome"]
