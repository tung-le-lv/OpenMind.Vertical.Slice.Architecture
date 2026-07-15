FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Payment.Api/Payment.Api.csproj src/Payment.Api/
RUN dotnet restore src/Payment.Api/Payment.Api.csproj
COPY src/Payment.Api/ src/Payment.Api/
RUN dotnet publish src/Payment.Api/Payment.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Payment.Api.dll"]
