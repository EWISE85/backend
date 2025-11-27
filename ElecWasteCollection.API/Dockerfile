# ======================= Stage 1: Build ===========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore từng dự án để tối ưu cache layer
COPY ElecWasteCollection.API/*.csproj ./ElecWasteCollection.API/
COPY ElecWasteCollection.Application/*.csproj ./ElecWasteCollection.Application/
COPY ElecWasteCollection.Domain/*.csproj ./ElecWasteCollection.Domain/
COPY ElecWasteCollection.Infrastructure/*.csproj ./ElecWasteCollection.Infrastructure/

# Nếu có file solution (.sln) hãy copy luôn
# COPY ElecWasteCollection.sln .

# Restore (ưu tiên folder API là entrypoint)
RUN dotnet restore ElecWasteCollection.API/ElecWasteCollection.API.csproj

# Copy toàn bộ source (sau restore để tối ưu cache)
COPY . .

# Chuyển context vào folder API
WORKDIR /src/ElecWasteCollection.API

# Publish (kiểu self-contained false để nhẹ, đã kèm native OpenCvSharp so)
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ======================= Stage 2: Runtime ===========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-focal AS runtime
# focal = Ubuntu 20.04, TƯƠNG THÍCH native OpenCvSharp for Linux!

# Cài thư viện native cần thiết cho OpenCvSharp
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libgl1-mesa-glx \
    libglib2.0-0 \
    libgtk2.0-0 \
    libxext6 \
    libxrender1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy from build stage
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]