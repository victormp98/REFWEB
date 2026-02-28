# ─────────────────────────────────────────────────────────────────
# Etapa 1: Build
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar (cache de capas)
COPY ["RefWeb.csproj", "./"]
RUN dotnet restore "RefWeb.csproj"

# Copiar todo y publicar
COPY . .
RUN dotnet publish "RefWeb.csproj" -c Release -o /app/publish --no-restore

# ─────────────────────────────────────────────────────────────────
# Etapa 2: Runtime
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Crear directorio de uploads (luego se monta como volumen en Coolify)
RUN mkdir -p /app/wwwroot/uploads \
    && mkdir -p /app/wwwroot/img/productos

# Copiar artefactos publicados
COPY --from=build /app/publish .

# Puerto de escucha (Coolify lo gestiona con su reverse proxy)
EXPOSE 8080

# Variables de entorno por defecto (se sobreescriben en Coolify)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RefWeb.dll"]
