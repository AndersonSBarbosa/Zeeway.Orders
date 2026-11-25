FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.sln ./
COPY src/Zeeway.Orders.Api/*.csproj src/Zeeway.Orders.Api/
COPY src/Zeeway.Orders.Domain/*.csproj src/Zeeway.Orders.Domain/
COPY src/Zeeway.Orders.Infrastructure/*.csproj src/Zeeway.Orders.Infrastructure/
COPY src/Zeeway.Orders.Service/*.csproj src/Zeeway.Orders.Service/
RUN dotnet restore src/Zeeway.Orders.Api/Zeeway.Orders.Api.csproj
COPY . .
RUN dotnet publish src/Zeeway.Orders.Api/Zeeway.Orders.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Zeeway.Orders.Api.dll"]
