FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ImmortalVault-Server.csproj", "./"]
RUN dotnet restore "./ImmortalVault-Server.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "ImmortalVault-Server.csproj" -c Release -o /app/build

# Публикуем приложение в режиме Release
FROM build AS publish
RUN dotnet publish "ImmortalVault-Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Запускаем приложение
ENTRYPOINT ["dotnet", "ImmortalVault-Server.dll"]
