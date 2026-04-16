# --- GIAI ĐOẠN 1: BUILD ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ElecWasteCollection.API/*.csproj ./ElecWasteCollection.API/
COPY ElecWasteCollection.Application/*.csproj ./ElecWasteCollection.Application/
COPY ElecWasteCollection.Domain/*.csproj ./ElecWasteCollection.Domain/
COPY ElecWasteCollection.Infrastructure/*.csproj ./ElecWasteCollection.Infrastructure/

RUN dotnet restore ElecWasteCollection.API/ElecWasteCollection.API.csproj -r linux-x64

COPY . .
WORKDIR /src/ElecWasteCollection.API

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false -r linux-x64 --self-contained false --no-restore

# Copy file .so
RUN find /root/.nuget/packages -name "libcvextern.so" | head -n 1 | xargs -I {} cp {} /app/publish/libcvextern.so

# --- GIAI ĐOẠN 2: RUNTIME ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime

# Cài đặt thư viện
RUN apt-get update && apt-get install -y --no-install-recommends \
    # Quan trọng nhất cho .NET Image processing
    libgdiplus \
    # Các thư viện đồ họa cơ bản mà OpenCV (Emgu) cần để xử lý bitmap
    libgl1 \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender1 \
    # Hỗ trợ font và xử lý hệ thống
    libfontconfig1 \
    libice6 \
    # Tối ưu toán học (giúp so sánh ảnh nhanh hơn)
    libopenblas-base \
    liblapack3 \
    # Runtime cần thiết
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Config môi trường
ENV DOTNET_SYSTEM_DRAWING_CHECK_SUPPORTED=false
ENV OPENCV_OPENCL_DEVICE=disabled

ENV LD_LIBRARY_PATH=/usr/lib:/app:$LD_LIBRARY_PATH

WORKDIR /app
COPY --from=build /app/publish .

# Copy file vào hệ thống
RUN cp libcvextern.so /usr/lib/libcvextern.so && chmod 755 /usr/lib/libcvextern.so

EXPOSE 8080
ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]