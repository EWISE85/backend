# Dùng base image .NET SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file csproj và phục hồi các gói NuGet
COPY ElecWasteCollection.API/*.csproj ./ElecWasteCollection.API/
COPY ElecWasteCollection.Application/*.csproj ./ElecWasteCollection.Application/
COPY ElecWasteCollection.Domain/*.csproj ./ElecWasteCollection.Domain/
COPY ElecWasteCollection.Infrastructure/*.csproj ./ElecWasteCollection.Infrastructure/
RUN dotnet restore ElecWasteCollection.API/ElecWasteCollection.API.csproj

# Copy toàn bộ mã nguồn vào container
COPY . .

# Build và publish ứng dụng
WORKDIR /src/ElecWasteCollection.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Dùng base image .NET Runtime cho runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Cài đặt các thư viện OpenCV và Emgu.CV runtime
RUN apt-get update && apt-get install -y \
    libopencv-dev \
    libpng-dev \
    libjpeg-dev \
    libtiff-dev \
    libdcmtk-dev \
    libfftw3-dev \
    libopencv-core-dev \
    libopencv-imgproc-dev

# Thêm thư mục chứa các thư viện .so vào biến môi trường LD_LIBRARY_PATH
ENV LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu:$LD_LIBRARY_PATH

# Chuyển thư mục làm việc đến /app
WORKDIR /app

# Copy các file đã build từ bước trước vào container
COPY --from=build /app/publish .

# Mở cổng 8080 cho ứng dụng
EXPOSE 8080

# Chạy ứng dụng khi container khởi động
ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]
