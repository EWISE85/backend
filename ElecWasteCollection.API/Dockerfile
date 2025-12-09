# --- GIAI ĐOẠN 1: BUILD ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ElecWasteCollection.API/*.csproj ./ElecWasteCollection.API/
COPY ElecWasteCollection.Application/*.csproj ./ElecWasteCollection.Application/
COPY ElecWasteCollection.Domain/*.csproj ./ElecWasteCollection.Domain/
COPY ElecWasteCollection.Infrastructure/*.csproj ./ElecWasteCollection.Infrastructure/

# Restore (Lúc này file .so sẽ nằm trong /root/.nuget/packages/...)
RUN dotnet restore ElecWasteCollection.API/ElecWasteCollection.API.csproj -r linux-x64

COPY . .
WORKDIR /src/ElecWasteCollection.API

# Publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false -r linux-x64 --self-contained false --no-restore

# === BƯỚC MỚI: TÌM VÀ COPY FILE .SO THỦ CÔNG ===
# Lệnh này tìm file libcvextern.so trong cache NuGet và copy thẳng vào thư mục publish
# Điều này đảm bảo file luôn có mặt, bất kể dotnet publish có làm sai hay không.
RUN find /root/.nuget/packages -name "libcvextern.so" | head -n 1 | xargs -I {} cp {} /app/publish/libcvextern.so

# --- GIAI ĐOẠN 2: RUNTIME ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime

# Cài đặt thư viện (Vẫn giữ nguyên bộ thư viện đầy đủ)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libgl1-mesa-glx \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender1 \
    libfontconfig1 \
    libice6 \
    libgtk2.0-0 \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_SYSTEM_DRAWING_CHECK_SUPPORTED=false
# Thêm đường dẫn hiện tại (/app) vào nơi tìm kiếm thư viện
ENV LD_LIBRARY_PATH=/app:$LD_LIBRARY_PATH

WORKDIR /app
COPY --from=build /app/publish .

# Kiểm tra lại lần cuối xem file đã đến nơi chưa (In ra log build)
RUN ls -la libcvextern.so && echo "Check: libcvextern.so OK"

EXPOSE 8080
ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]