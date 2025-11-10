# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: Build and restore dependencies
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the project file first (for caching restore layer)
COPY ["IntegratedAPI.csproj", "./"]

# Restore project dependencies
RUN dotnet restore "IntegratedAPI.csproj"

# Copy the rest of the application source code
COPY . .

# Build the application
RUN dotnet build "IntegratedAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish for deployment
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IntegratedAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IntegratedAPI.dll"]