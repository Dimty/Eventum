FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Eventum.Users.Domain/Eventum.Users.Domain.csproj Eventum.Users.Domain/
COPY Eventum.Users.Application/Eventum.Users.Application.csproj Eventum.Users.Application/
COPY Eventum.Users.Infrastructure/Eventum.Users.Infrastructure.csproj Eventum.Users.Infrastructure/
COPY Eventum.Users.WebApi/Eventum.Users.WebApi.csproj Eventum.Users.WebApi/
RUN dotnet restore Eventum.Users.WebApi/Eventum.Users.WebApi.csproj

COPY . .
RUN dotnet publish Eventum.Users.WebApi/Eventum.Users.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Eventum.Users.WebApi.dll"]
