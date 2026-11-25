FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.sln ./
COPY src/Zeeway.Pedidos.Api/*.csproj src/Zeeway.Pedidos.Api/
COPY src/Zeeway.Pedidos.Domain/*.csproj src/Zeeway.Pedidos.Domain/
COPY src/Zeeway.Pedidos.Infrastructure/*.csproj src/Zeeway.Pedidos.Infrastructure/
COPY src/Zeeway.Pedidos.Service/*.csproj src/Zeeway.Pedidos.Service/
RUN dotnet restore src/Zeeway.Pedidos.Api/Zeeway.Pedidos.Api.csproj
COPY . .
RUN dotnet publish src/Zeeway.Pedidos.Api/Zeeway.Pedidos.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Zeeway.Pedidos.Api.dll"]
