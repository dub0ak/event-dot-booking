FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY Directory.Packages.props ./

COPY src/Services/Users/EBooking.Users.Domain/EBooking.Users.Domain.csproj \
     src/Services/Users/EBooking.Users.Domain/

COPY src/Services/Users/EBooking.Users.Application/EBooking.Users.Application.csproj \
     src/Services/Users/EBooking.Users.Application/

COPY src/Services/Users/EBooking.Users.Infrastructure/EBooking.Users.Infrastructure.csproj \
     src/Services/Users/EBooking.Users.Infrastructure/

COPY src/Services/Users/EBooking.Users.Api/EBooking.Users.Api.csproj \
     src/Services/Users/EBooking.Users.Api/

RUN dotnet restore \
    src/Services/Users/EBooking.Users.Api/EBooking.Users.Api.csproj

COPY . .

RUN dotnet publish \
    src/Services/Users/EBooking.Users.Api/EBooking.Users.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


FROM runtime AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EBooking.Users.Api.dll"]