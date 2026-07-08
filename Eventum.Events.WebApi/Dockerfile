FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Eventum.Shared.Contracts/Eventum.Shared.Contracts.csproj Eventum.Shared.Contracts/
COPY Eventum.Events.Domain/Eventum.Events.Domain.csproj Eventum.Events.Domain/
COPY Eventum.Events.Application/Eventum.Events.Application.csproj Eventum.Events.Application/
COPY Eventum.Events.Infrastructure/Eventum.Events.Infrastructure.csproj Eventum.Events.Infrastructure/
COPY Eventum.Events.WebApi/Eventum.Events.WebApi.csproj Eventum.Events.WebApi/
RUN dotnet restore Eventum.Events.WebApi/Eventum.Events.WebApi.csproj

COPY . .
RUN dotnet publish Eventum.Events.WebApi/Eventum.Events.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Eventum.Events.WebApi.dll"]
