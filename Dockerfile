# Используем многоэтапную сборку
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Устанавливаем сертификаты для HTTPS
RUN apt-get update && \
    apt-get install -y openssl && \
    mkdir -p /app/certs && \
    openssl req -x509 -newkey rsa:4096 -nodes \
    -keyout /app/certs/aspnetapp.key \
    -out /app/certs/aspnetapp.crt \
    -subj "/CN=localhost" -days 365 && \
    openssl pkcs12 -export -out /app/certs/aspnetapp.pfx \
    -inkey /app/certs/aspnetapp.key \
    -in /app/certs/aspnetapp.crt \
    -passout pass:YourPassword123! && \
    chmod 644 /app/certs/*

EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["GameTeam/GameTeam.csproj", "GameTeam/"]
RUN dotnet restore "GameTeam/GameTeam.csproj"
COPY . .
WORKDIR "/src/GameTeam"
RUN dotnet build "GameTeam.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameTeam.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Копируем сертификаты
COPY --from=base /app/certs /app/certs

# Устанавливаем переменные среды для HTTPS
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword123!
ENV ASPNETCORE_URLS=https://+:443;http://+:80

ENTRYPOINT ["dotnet", "GameTeam.dll"]