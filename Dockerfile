# Etap 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy pliki projektu i wszystkie foldery ręcznie
COPY *.csproj ./
COPY Services ./Services
COPY Controllers ./Controllers
COPY Middleware ./Middleware
COPY Models ./Models
COPY *.cs ./ 

# Restore i publish
RUN dotnet restore ./ArWidgetApi.csproj
RUN dotnet publish ./ArWidgetApi.csproj -c Release -o /app/publish

# Etap 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ArWidgetApi.dll"]
