# Stage 1: Build .NET 8
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file spesifik sesuai struktur kamu
COPY absensi.csproj ./
RUN dotnet restore

# Copy seluruh source code
COPY . ./
RUN dotnet publish "absensi.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

# Nama DLL disesuaikan dengan absensi.csproj
ENTRYPOINT ["dotnet", "absensi.dll"]
