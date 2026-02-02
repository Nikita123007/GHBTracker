# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj .
RUN dotnet restore

# Копируем остальные файлы и публикуем
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Этап рантайма
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Копируем собранное приложение
COPY --from=build /app/publish .

# Точка входа (замени имя DLL на своё)
ENTRYPOINT ["dotnet", "GHBTracker.dll"]