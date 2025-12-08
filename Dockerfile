# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy csproj i przywracamy paczki
COPY *.csproj ./
RUN dotnet restore

# Kopiujemy wszystkie pliki projektu
COPY . .

# Publikujemy projekt do folderu /app/publish
RUN dotnet publish -c Release -o /app/publish

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Kopiujemy opublikowaną aplikację
COPY --from=build /app/publish .

# Ustawiamy entrypoint
ENTRYPOINT ["dotnet", "ArWidgetApi.dll"]
