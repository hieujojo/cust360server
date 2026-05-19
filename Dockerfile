# ============================================================
# Stage 1: Build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies trước (tận dụng Docker layer cache)
COPY ["CRM.Api.csproj", "./"]
RUN dotnet restore "CRM.Api.csproj"

# Copy toàn bộ source code
COPY . .

# Build và publish
RUN dotnet publish "CRM.Api.csproj" -c Release -o /app/publish --no-restore

# ============================================================
# Stage 2: Runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy output từ build stage
COPY --from=build /app/publish .

# Railway inject PORT qua env var, ASP.NET Core đọc từ ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

ENTRYPOINT ["dotnet", "CRM.Api.dll"]
