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

# === CẬP NHẬT: CÀI ĐẶT ĐẦY ĐỦ THƯ VIỆN CHO OPENCV ===
# Dùng --no-install-recommends để tránh cài các gói không cần thiết (như máy in, âm thanh)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libgl1-mesa-glx \
    libglib2.0-0 \
    libgtk2.0-0 \
    libxext6 \
    libxrender1
# ===================================================

WORKDIR /app
COPY --from-build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]