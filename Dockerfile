# Etapa 1: Compilación y publicación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivo .csproj y restaurar dependencias
COPY ["ApiAutoLavado/ApiAutoLavado.csproj", "ApiAutoLavado/"]
RUN dotnet restore "ApiAutoLavado/ApiAutoLavado.csproj"

# Copiar todo el código fuente y compilar
COPY . .
WORKDIR "/src/ApiAutoLavado"
RUN dotnet publish "ApiAutoLavado.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen de ejecución (ligera)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto estándar HTTP
EXPOSE 8080

# Render inyecta PORT; se usa ese puerto si existe, si no 8080.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet ApiAutoLavado.dll"]