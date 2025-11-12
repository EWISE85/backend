# Stage 1: Build - Sử dụng SDK để khôi phục và biên dịch
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép các tệp .csproj và file solution (.sln) nếu có
COPY ElecWasteCollection.API/*.csproj ./ElecWasteCollection.API/
COPY ElecWasteCollection.Application/*.csproj ./ElecWasteCollection.Application/
COPY ElecWasteCollection.Domain/*.csproj ./ElecWasteCollection.Domain/
COPY ElecWasteCollection.Infrastructure/*.csproj ./ElecWasteCollection.Infrastructure/
# COPY ElecWasteCollection.sln .

# Chạy restore (sử dụng dự án API)
RUN dotnet restore ElecWasteCollection.API/ElecWasteCollection.API.csproj

# Sao chép toàn bộ mã nguồn
COPY . .

# Chuyển context sang dự án API
WORKDIR /src/ElecWasteCollection.API

# Chạy lệnh PUBLISH
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ---

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# === THÊM VÀO: CÀI ĐẶT THƯ VIỆN CHO OPENCV ===
# Cài đặt các dependencies native mà OpenCvSharp cần trên Linux (Debian/Ubuntu)
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libgl1-mesa-glx
# ============================================

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]