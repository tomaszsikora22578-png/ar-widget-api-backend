FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Skopiuj projekt csproj
COPY *.csproj ./

# Debug: pokaż zawartość workspace przed kopiowaniem Services
RUN echo "=== ZAWARTOŚĆ KATALOGU PRZED KOPIOWANIEM SERVICES ===" && ls -R

# Skopiuj foldery
COPY Services ./Services
COPY Controllers ./Controllers
COPY Middleware ./Middleware
COPY Models ./Models
COPY *.cs ./

# Debug: pokaż zawartość po skopiowaniu Services
RUN echo "=== ZAWARTOŚĆ KATALOGU PO KOPIOWANIU SERVICES ===" && ls -R

# Restore i publish
RUN dotnet restore ./ArWidgetApi.csproj
RUN dotnet publish ./ArWidgetApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ArWidgetApi.dll"]
