FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.sln ./
COPY Zeeway.Orders.Api/*.csproj Zeeway.Orders.Api/
COPY Zeeway.Orders.Domain/*.csproj Zeeway.Orders.Domain/
COPY Zeeway.Orders.Infrastructure/*.csproj Zeeway.Orders.Infrastructure/
COPY Zeeway.Orders.Tests/*.csproj Zeeway.Orders.Tests/
RUN dotnet restore Zeeway.Orders.Api/Zeeway.Orders.Api.csproj
COPY . .
RUN dotnet publish Zeeway.Orders.Api/Zeeway.Orders.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Zeeway.Orders.Api.dll"]
