# -------------------------
# 1. Base Runtime Image
# -------------------------
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 8080

# -------------------------
# 2. Build Image (SDK)
# -------------------------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy only the csproj first (helps cache Docker layers)
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the project files
COPY . .

# Build and publish the project
RUN dotnet publish -c Release -o /app/publish

# -------------------------
# 3. Final Image
# -------------------------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Start the backend
ENTRYPOINT ["dotnet", "Backend.dll"]
