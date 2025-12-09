# --- GIAI ĐOẠN 1: BUILD ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file dự án
COPY ElecWasteCollection.API/*.csproj ./ElecWasteCollection.API/
COPY ElecWasteCollection.Application/*.csproj ./ElecWasteCollection.Application/
COPY ElecWasteCollection.Domain/*.csproj ./ElecWasteCollection.Domain/
COPY ElecWasteCollection.Infrastructure/*.csproj ./ElecWasteCollection.Infrastructure/

# QUAN TRỌNG: Thêm -r linux-x64 vào đây để nó tải đúng gói EmguCV cho Linux
RUN dotnet restore ElecWasteCollection.API/ElecWasteCollection.API.csproj -r linux-x64

# Copy toàn bộ source code
COPY . .

WORKDIR /src/ElecWasteCollection.API

# Publish với cùng runtime identifier đã restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false -r linux-x64 --self-contained false --no-restore

# --- GIAI ĐOẠN 2: RUNTIME ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Cài đặt các thư viện hệ thống cần thiết cho EmguCV & System.Drawing
# libgdiplus: Cho System.Drawing
# libgl1-mesa-glx, libglib2.0-0: Cho OpenCV native
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libgl1-mesa-glx \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender-dev \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Fix lỗi không tìm thấy thư viện GDI+
RUN ln -s /usr/lib/x86_64-linux-gnu/libgdiplus.so /usr/lib/libgdiplus.so

# Bắt buộc: Tắt kiểm tra support System.Drawing trên .NET 8 Linux
ENV DOTNET_SYSTEM_DRAWING_CHECK_SUPPORTED=false

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]