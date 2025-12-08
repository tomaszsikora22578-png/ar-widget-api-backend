# 1️⃣ Wybór obrazu SDK do builda
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 2️⃣ Kopiujemy tylko plik projektu i przywracamy pakiety
COPY ArWidgetApi.csproj ./
RUN dotnet restore ArWidgetApi.csproj

# 3️⃣ Kopiujemy wszystkie pozostałe pliki projektu
COPY . .

# 4️⃣ Publikujemy projekt w trybie Release
RUN dotnet publish ArWidgetApi.csproj -c Release -o /app/publish

# ===============================

# 5️⃣ Obraz runtime (mniejszy, do uruchomienia aplikacji)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 6️⃣ Kopiujemy pliki opublikowane z builda
COPY --from=build /app/publish .

# 7️⃣ Ustawienie zmiennej środowiskowej dla Google Secret Manager
# Przykładowo nazwa secreta: firebase-admin-key
# Cloud Run: dodaj secret do serwisu i ustaw ENV: FIREBASE_SECRET_PATH=/secrets/firebase-admin-key.json
ENV FIREBASE_SECRET_PATH=/secrets/firebase-admin-key.json

# 8️⃣ Otwieramy port
EXPOSE 8080

# 9️⃣ Uruchamiamy aplikację
ENTRYPOINT ["dotnet", "ArWidgetApi.dll"]
