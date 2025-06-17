# Imagen base solo con el runtime ASP.NET Core 9.0 Preview
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS base

# Establece el directorio de trabajo dentro del contenedor
WORKDIR /app

# Copia los archivos ya publicados desde tu máquina local al contenedor
COPY ./publish .

# Expone el puerto 80
EXPOSE 80

# Comando para iniciar tu aplicación
ENTRYPOINT ["dotnet", "Barber.Maui.API.dll"]
