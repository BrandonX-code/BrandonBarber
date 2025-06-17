# Imagen base ASP.NET Core 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Imagen SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Barber.Maui.API/Barber.Maui.API.csproj", "Barber.Maui.API/"]
RUN dotnet restore "Barber.Maui.API/Barber.Maui.API.csproj"
COPY . .
WORKDIR "/src/Barber.Maui.API"
RUN dotnet build "Barber.Maui.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Barber.Maui.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Barber.Maui.API.dll"]
