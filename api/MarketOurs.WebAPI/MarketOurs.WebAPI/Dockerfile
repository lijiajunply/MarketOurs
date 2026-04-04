FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MarketOurs.WebAPI/MarketOurs.WebAPI.csproj", "MarketOurs.WebAPI/"]
RUN dotnet restore "MarketOurs.WebAPI/MarketOurs.WebAPI.csproj"
COPY . .
WORKDIR "/src/MarketOurs.WebAPI"
RUN dotnet build "./MarketOurs.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MarketOurs.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarketOurs.WebAPI.dll"]
