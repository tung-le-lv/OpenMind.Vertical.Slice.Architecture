FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Order.Api/Order.Api.csproj src/Order.Api/
RUN dotnet restore src/Order.Api/Order.Api.csproj
COPY src/Order.Api/ src/Order.Api/
RUN dotnet publish src/Order.Api/Order.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Order.Api.dll"]
