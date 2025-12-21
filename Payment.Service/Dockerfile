FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

# восстанавливаем зависимости
RUN dotnet restore Payment.Service/Payment.Service.csproj

# публикуем сервис
RUN dotnet publish Payment.Service/Payment.Service.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Payment.Service.dll"]