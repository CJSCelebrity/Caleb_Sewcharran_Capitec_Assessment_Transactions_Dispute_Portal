FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Capitec_Transactions_Disputes_Backend/Capitec_Transactions_Disputes_Backend.csproj", "Capitec_Transactions_Disputes_Backend/"]
RUN dotnet restore "Capitec_Transactions_Disputes_Backend/Capitec_Transactions_Disputes_Backend.csproj"
COPY . .
WORKDIR "/src/Capitec_Transactions_Disputes_Backend"
RUN dotnet build "./Capitec_Transactions_Disputes_Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Capitec_Transactions_Disputes_Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Capitec_Transactions_Disputes_Backend.dll"]
