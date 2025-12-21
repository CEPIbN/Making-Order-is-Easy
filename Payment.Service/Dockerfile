FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

# восстанавливаем зависимости
RUN dotnet restore OrderService/OrderService.csproj

# публикуем сервис
RUN dotnet publish Inventory.Service/Inventory.Service.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Payment.Service.dll"]