# Sử dụng .NET SDK để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy file csproj vào container để build
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ source code và build
COPY . ./
RUN dotnet publish -c Release -o out

# Sử dụng .NET runtime để chạy app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Set cổng chạy app
EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "VaccinaCare.API.dll"]
