FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Service Inventory Worker/Q10.Prueba.Inventory.Worker/Q10.Prueba.Inventory.Worker.csproj"
RUN dotnet publish "Service Inventory Worker/Q10.Prueba.Inventory.Worker/Q10.Prueba.Inventory.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Q10.Prueba.Inventory.Worker.dll"]
