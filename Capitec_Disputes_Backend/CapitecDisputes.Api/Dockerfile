FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CapitecDisputes.Api/CapitecDisputes.Api.csproj", "CapitecDisputes.Api/"]
COPY ["CapitecDisputes.Application/CapitecDisputes.Application.csproj", "CapitecDisputes.Application/"]
COPY ["CapitecDisputes.Domain/CapitecDisputes.Domain.csproj", "CapitecDisputes.Domain/"]
COPY ["CapitecDisputes.Infrastructure/CapitecDisputes.Infrastructure.csproj", "CapitecDisputes.Infrastructure/"]
RUN dotnet restore "CapitecDisputes.Api/CapitecDisputes.Api.csproj"
COPY . .
WORKDIR "/src/CapitecDisputes.Api"
RUN dotnet build "./CapitecDisputes.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CapitecDisputes.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CapitecDisputes.Api.dll"]
