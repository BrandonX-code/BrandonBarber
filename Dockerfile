FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia todo el código del repo
COPY . .

# Publica el proyecto principal, pero indicando la ruta correcta
RUN dotnet publish Barber.Maui.API/Barber.Maui.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Barber.Maui.API.dll"]
