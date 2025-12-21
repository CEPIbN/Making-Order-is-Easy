FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

# восстанавливаем зависимости
RUN dotnet restore Order.Service/Order.Service.csproj

# публикуем сервис
RUN dotnet publish Order.Service/Order.Service.csproj \
    -c Release \
    -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Order.Service.dll"]