FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV IPTV_ACCOUNTS="AccountName;https://account.host.com/"
ENV CATEGORY_FILTERS="UK|;US|;UK |;US |;USA|;USA |;PPV "
ENV ADULT_FILTER="true"
ENV CACHE_TIME="60"
ENV EPG_TIME="1440"
ENV M3U_TIME="720"

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IptvMan/IptvMan.csproj", "IptvMan/"]
RUN dotnet restore "IptvMan/IptvMan.csproj"
COPY . .
WORKDIR "/src/IptvMan"
RUN dotnet build "./IptvMan.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IptvMan.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IptvMan.dll"]
