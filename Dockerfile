# Usa la imagen base de ASP.NET 9.0 Preview
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS base
WORKDIR /app
EXPOSE 80

# Imagen con SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

# Copia solo el archivo .csproj de la API
COPY ["Barber.Maui.API/Barber.Maui.API.csproj", "Barber.Maui.API/"]

# Restaura las dependencias
RUN dotnet restore "Barber.Maui.API/Barber.Maui.API.csproj"

# Copia el resto del código
COPY . .

# Establece el directorio de trabajo del proyecto
WORKDIR "/src/Barber.Maui.API"
RUN dotnet build "Barber.Maui.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Barber.Maui.API.csproj" -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Barber.Maui.API.dll"]