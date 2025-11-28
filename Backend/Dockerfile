# -------------------------
# 1. Build Image (SDK)
# -------------------------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Install Entity Framework tools (version 7.0.x for .NET 7.0)
RUN dotnet tool install --global dotnet-ef --version 7.0.14

# Copy only the csproj first (helps cache Docker layers)
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the project files
COPY . .

# Build and publish the project
RUN dotnet publish -c Release -o /app/publish

# -------------------------
# 2. Final Image (with SDK for migrations)
# -------------------------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS final
WORKDIR /app

# Install Entity Framework tools (version 7.0.x for .NET 7.0)
RUN dotnet tool install --global dotnet-ef --version 7.0.14
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy source files for EF migrations
COPY --from=build /src . 

# Copy the published app from build stage
COPY --from=build /app/publish .

# Start the backend
ENTRYPOINT ["dotnet", "Backend.dll"]
