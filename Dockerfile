# ============================================================
# Stage 1: Build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies trước (tận dụng Docker layer cache)
# Layer này chỉ rebuild khi csproj thay đổi
COPY ["CRM.Api.csproj", "./"]
RUN dotnet restore "CRM.Api.csproj"

# Copy source code và publish
COPY . .
RUN dotnet publish "CRM.Api.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============================================================
# Stage 2: Runtime — image nhỏ hơn, không có SDK
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Chạy với non-root user để tăng bảo mật
RUN adduser --disabled-password --gecos '' appuser
WORKDIR /app

# Copy output từ build stage
COPY --from=build /app/publish .

# Đổi owner về appuser
RUN chown -R appuser:appuser /app
USER appuser

# Render inject PORT động, cần đọc từ biến môi trường

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "CRM.Api.dll"]
