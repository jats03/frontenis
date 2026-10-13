FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Frontenis.sln ./
COPY src/Frontenis.Domain/*.csproj src/Frontenis.Domain/
COPY src/Frontenis.Application/*.csproj src/Frontenis.Application/
COPY src/Frontenis.Infrastructure/*.csproj src/Frontenis.Infrastructure/
COPY src/Frontenis.Api/*.csproj src/Frontenis.Api/
RUN dotnet restore src/Frontenis.Api/Frontenis.Api.csproj
COPY src/ src/
RUN dotnet publish src/Frontenis.Api/Frontenis.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render inyecta PORT; en local usa 8080
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Frontenis.Api.dll"]
