# Imagen base con el runtime de ASP.NET 9.0.102
FROM mcr.microsoft.com/dotnet/aspnet:9.0.102 AS base
WORKDIR /app
EXPOSE 80

# Imagen con el SDK exacto para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0.102 AS build
WORKDIR /src

# Copia solo el .csproj y restaura dependencias
COPY ["Barber.Maui.API/Barber.Maui.API.csproj", "Barber.Maui.API/"]
RUN dotnet restore "Barber.Maui.API/Barber.Maui.API.csproj"

# Copia todo lo demás
COPY . .
WORKDIR "/src/Barber.Maui.API"
RUN dotnet build "Barber.Maui.API.csproj" -c Release -o /app/build

# Publica
FROM build AS publish
RUN dotnet publish "Barber.Maui.API.csproj" -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Barber.Maui.API.dll"]
