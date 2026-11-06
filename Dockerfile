# SDK de .NET 10 para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar dependencias (aprovecha la caché de capas)
COPY ["LoboEsteparioAPI.csproj", "./"]
RUN dotnet restore "LoboEsteparioAPI.csproj"

# Copiar el código fuente y publicar
COPY . .
RUN dotnet publish "LoboEsteparioAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime sobre Debian (glibc) para soporte nativo de DuckDB
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# HOME con caché de extensiones DuckDB (httpfs, spatial)
ENV HOME=/app
RUN mkdir -p /app/.duckdb && chmod -R 777 /app/.duckdb

COPY --from=build /app/publish .

# Render puede sobrescribir ASPNETCORE_URLS por variable de entorno
ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080

ENTRYPOINT ["dotnet", "LoboEsteparioAPI.dll"]
