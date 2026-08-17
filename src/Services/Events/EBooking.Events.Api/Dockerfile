FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY Directory.Packages.props ./

COPY src/Blocks/EBooking.Contracts/EBooking.Contracts.csproj \
     src/Blocks/EBooking.Contracts/

COPY src/Services/Events/EBooking.Events.Domain/EBooking.Events.Domain.csproj \
     src/Services/Events/EBooking.Events.Domain/

COPY src/Services/Events/EBooking.Events.Application/EBooking.Events.Application.csproj \
     src/Services/Events/EBooking.Events.Application/

COPY src/Services/Events/EBooking.Events.Infrastructure/EBooking.Events.Infrastructure.csproj \
     src/Services/Events/EBooking.Events.Infrastructure/

COPY src/Services/Events/EBooking.Events.Api/EBooking.Events.Api.csproj \
     src/Services/Events/EBooking.Events.Api/

RUN dotnet restore \
    src/Services/Events/EBooking.Events.Api/EBooking.Events.Api.csproj

COPY . .

RUN dotnet publish \
    src/Services/Events/EBooking.Events.Api/EBooking.Events.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


FROM runtime AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EBooking.Events.Api.dll"]