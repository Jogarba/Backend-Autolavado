# Etapa 1: Compilación y publicación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivo .csproj y restaurar dependencias
COPY ["ApiAutoLavado/ApiAutoLavado.csproj", "ApiAutoLavado/"]
RUN dotnet restore "ApiAutoLavado/ApiAutoLavado.csproj"

# Copiar todo el código fuente y compilar
COPY . .
WORKDIR "/src/ApiAutoLavado"
RUN dotnet publish "ApiAutoLavado.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen de ejecución (ligera)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto estándar HTTP
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ApiAutoLavado.dll"]