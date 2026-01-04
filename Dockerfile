# Stage 1: Build Frontend
FROM node:22-alpine AS frontend
WORKDIR /src
COPY IptvManFrontend/package*.json ./
RUN npm ci
COPY IptvManFrontend/ ./
RUN npm run build

# Stage 2: Build Backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IptvMan/IptvMan.csproj", "IptvMan/"]
RUN dotnet restore "IptvMan/IptvMan.csproj"
COPY IptvMan/ IptvMan/
WORKDIR "/src/IptvMan"
RUN dotnet build "IptvMan.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish Backend
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IptvMan.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=4050
EXPOSE 4050

# Copy backend
COPY --from=publish /app/publish .

# Copy frontend to wwwroot
COPY --from=frontend /src/build ./wwwroot

ENTRYPOINT ["dotnet", "IptvMan.dll"]