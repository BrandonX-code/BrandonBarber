FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar el archivo .csproj desde su ruta correcta
COPY Barber.Maui.API/Barber.Maui.API.csproj ./
RUN dotnet restore Barber.Maui.API.csproj

# Copiar todo el código fuente
COPY . ./
WORKDIR /app/Barber.Maui.API
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/Barber.Maui.API/out ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "Barber.Maui.API.dll"]
