FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Eventum.Shared.Contracts/Eventum.Shared.Contracts.csproj Eventum.Shared.Contracts/
COPY Eventum.Bookings.Domain/Eventum.Bookings.Domain.csproj Eventum.Bookings.Domain/
COPY Eventum.Bookings.Application/Eventum.Bookings.Application.csproj Eventum.Bookings.Application/
COPY Eventum.Bookings.Infrastructure/Eventum.Bookings.Infrastructure.csproj Eventum.Bookings.Infrastructure/
COPY Eventum.Bookings.WebApi/Eventum.Bookings.WebApi.csproj Eventum.Bookings.WebApi/
RUN dotnet restore Eventum.Bookings.WebApi/Eventum.Bookings.WebApi.csproj

COPY . .
RUN dotnet publish Eventum.Bookings.WebApi/Eventum.Bookings.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Eventum.Bookings.WebApi.dll"]
