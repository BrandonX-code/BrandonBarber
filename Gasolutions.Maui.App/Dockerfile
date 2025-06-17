# Usa imagen base de .NET 9
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src
COPY ["BrandonBarber/BrandonBarber.csproj", "BrandonBarber/"]
RUN dotnet restore "BrandonBarber/BrandonBarber.csproj"
COPY . .
WORKDIR /src/BrandonBarber
RUN dotnet build "BrandonBarber.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src/BrandonBarber
RUN dotnet publish "BrandonBarber.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BrandonBarber.dll"]
