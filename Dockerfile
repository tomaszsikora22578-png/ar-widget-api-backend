# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Skopiuj plik csproj i przywróć zależności
COPY *.csproj .
RUN dotnet restore
# Skopiuj pozostałe pliki i zbuduj aplikację
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Cloud Run wymaga nasłuchiwania na porcie 8080 (domyślnie, jeśli nie zmieniono)
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ArWidgetApi.dll"]