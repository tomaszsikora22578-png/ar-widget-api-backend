
# Stage 1: Build the application
# Użyj stabilnej obrazu SDK .NET 8.0 lub innej wersji, której używasz
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiowanie i przywracanie zależności
COPY *.csproj .
RUN dotnet restore

# Kopiowanie reszty kodu i budowanie
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Cloud Run domyślnie nasłuchuje na porcie 8080 (wymagane w kontenerze)
ENV ASPNETCORE_URLS=http://+:8080 
EXPOSE 8080
COPY --from=build /app/publish .
# Uruchomienie aplikacji
ENTRYPOINT ["dotnet", "ArWidgetApi.dll"] 
# Zmień "ArWidgetApi.dll" na nazwę pliku DLL Twojego głównego projektu
