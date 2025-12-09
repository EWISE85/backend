# --- BUILD ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ElecWasteCollection.API/*.csproj ./ElecWasteCollection.API/
COPY ElecWasteCollection.Application/*.csproj ./ElecWasteCollection.Application/
COPY ElecWasteCollection.Domain/*.csproj ./ElecWasteCollection.Domain/
COPY ElecWasteCollection.Infrastructure/*.csproj ./ElecWasteCollection.Infrastructure/

# Restore bản 4.9 nhẹ hơn
RUN dotnet restore ElecWasteCollection.API/ElecWasteCollection.API.csproj -r linux-x64

COPY . .
WORKDIR /src/ElecWasteCollection.API

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false -r linux-x64 --self-contained false --no-restore

# Vẫn giữ lệnh copy thủ công cho chắc chắn
RUN find /root/.nuget/packages -name "libcvextern.so" | head -n 1 | xargs -I {} cp {} /app/publish/libcvextern.so

# --- RUNTIME (Dùng bản Debian mặc định - Nhẹ nhất) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Với EmguCV 4.9, chỉ cần cài những thứ cơ bản này là chạy được
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libgl1 \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender1 \
    libfontconfig1 \
    libice6 \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_SYSTEM_DRAWING_CHECK_SUPPORTED=false
ENV LD_LIBRARY_PATH=/usr/lib:/app:$LD_LIBRARY_PATH

WORKDIR /app
COPY --from=build /app/publish .

# Copy file .so vào hệ thống
RUN cp libcvextern.so /usr/lib/libcvextern.so && chmod 755 /usr/lib/libcvextern.so

EXPOSE 8080
ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]