FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VaccinaCare.API/VaccinaCare.API.csproj", "VaccinaCare.API/"]
COPY ["VaccinaCare.Application/VaccinaCare.Application.csproj", "VaccinaCare.Application/"]
COPY ["VaccinaCare.Repository/VaccinaCare.Repository.csproj", "VaccinaCare.Repository/"]
COPY ["VaccinaCare.Domain/VaccinaCare.Domain.csproj", "VaccinaCare.Domain/"]
RUN dotnet restore "VaccinaCare.API/VaccinaCare.API.csproj"
COPY . .
WORKDIR "/src/VaccinaCare.API"
RUN dotnet build "VaccinaCare.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VaccinaCare.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VaccinaCare.API.dll"]
