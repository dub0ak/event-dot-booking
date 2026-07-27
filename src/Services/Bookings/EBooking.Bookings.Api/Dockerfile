FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY Directory.Packages.props ./

COPY src/Blocks/EBooking.Contracts/EBooking.Contracts.csproj \
     src/Blocks/EBooking.Contracts/

COPY src/Services/Bookings/EBooking.Bookings.Domain/EBooking.Bookings.Domain.csproj \
     src/Services/Bookings/EBooking.Bookings.Domain/

COPY src/Services/Bookings/EBooking.Bookings.Application/EBooking.Bookings.Application.csproj \
     src/Services/Bookings/EBooking.Bookings.Application/

COPY src/Services/Bookings/EBooking.Bookings.Infrastructure/EBooking.Bookings.Infrastructure.csproj \
     src/Services/Bookings/EBooking.Bookings.Infrastructure/

COPY src/Services/Bookings/EBooking.Bookings.Api/EBooking.Bookings.Api.csproj \
     src/Services/Bookings/EBooking.Bookings.Api/

RUN dotnet restore \
    src/Services/Bookings/EBooking.Bookings.Api/EBooking.Bookings.Api.csproj

COPY . .

RUN dotnet publish \
    src/Services/Bookings/EBooking.Bookings.Api/EBooking.Bookings.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


FROM runtime AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EBooking.Bookings.Api.dll"]