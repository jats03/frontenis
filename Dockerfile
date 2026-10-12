# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Frontenis.sln ./
COPY src/ src/
COPY tests/ tests/
RUN dotnet publish src/Frontenis.Api/Frontenis.Api.csproj -c Release -o /app

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render indica el puerto en la variable PORT (por defecto 10000)
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} exec dotnet Frontenis.Api.dll"]
