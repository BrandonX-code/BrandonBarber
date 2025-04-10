# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos toda la solución (necesario para que detecte las referencias entre proyectos)
COPY . .

# Restauramos usando la solución
RUN dotnet restore BrandonBarber.sln

# Publicamos solo el proyecto de la API
RUN dotnet publish Barber.Maui.API/Barber.Maui.API.csproj -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiamos lo publicado desde la etapa build
COPY --from=build /app/publish ./

# Exponemos el puerto 8080 para Render
EXPOSE 8080

# Ejecutamos la API
ENTRYPOINT ["dotnet", "Barber.Maui.API.dll"]
