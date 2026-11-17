FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore DatabaseInitializer/Q10.Prueba.Tienda.DatabaseInitializer.csproj
RUN dotnet publish DatabaseInitializer/Q10.Prueba.Tienda.DatabaseInitializer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY Database/01_CreateDatabase.sql /app/Database/01_CreateDatabase.sql
COPY Database/02_CreateSchemas.sql /app/Database/02_CreateSchemas.sql
ENTRYPOINT ["dotnet", "Q10.Prueba.Tienda.DatabaseInitializer.dll"]
