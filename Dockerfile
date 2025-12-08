# SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Skopiuj tylko plik csproj i przywróć zależności
COPY ArWidgetApi.csproj ./
RUN dotnet restore

# Skopiuj cały projekt
COPY . ./

# Publish tylko projektu, nie całej solucji
RUN dotnet publish ArWidgetApi.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ArWidgetApi.dll"]
