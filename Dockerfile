# 1) Build-Stage mit dem .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Nur Projektdateien kopieren, damit restore gecached wird
COPY UrlHealthCheckFunction.csproj ./
# ggf. Models-Ordner einbeziehen, falls csproj dort referenziert
COPY Models/*.csproj ./Models/

RUN dotnet restore UrlHealthCheckFunction.csproj

# Den Rest des Codes kopieren
COPY . .

# Publish der prekompilierten Function in Release-Konfiguration
RUN dotnet publish UrlHealthCheckFunction.csproj \
    -c Release \
    -o /app/publish

# 2) Laufzeit-Stage mit Azure Functions Runtime
FROM mcr.microsoft.com/azure-functions/dotnet:4
WORKDIR /home/site/wwwroot

# Aus der Build-Stage die publizierten Dateien holen
COPY --from=build /app/publish .

# Port und Startkommando sind im Base-Image vorkonfiguriert
