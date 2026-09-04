# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY LearnSphere.csproj .
RUN dotnet restore LearnSphere.csproj

COPY . .
RUN dotnet publish LearnSphere.csproj --no-restore --configuration Release --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# The SQLite database file and uploaded lesson files live under the working
# directory - fine for a demo deployment, but note it's ephemeral: anything
# written here is lost on container restart/redeploy unless a persistent
# volume is mounted over /app.
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LearnSphere.dll"]
