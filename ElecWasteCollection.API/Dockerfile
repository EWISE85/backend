# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép các csproj và phục hồi các phụ thuộc
COPY ["ElecWasteCollection.sln", "./"]
COPY ["ElecWasteCollection.API/ElecWasteCollection.API.csproj", "ElecWasteCollection.API/"]
COPY ["ElecWasteCollection.Application/ElecWasteCollection.Application.csproj", "ElecWasteCollection.Application/"]
COPY ["ElecWasteCollection.Domain/ElecWasteCollection.Domain.csproj", "ElecWasteCollection.Domain/"]
COPY ["ElecWasteCollection.Infrastructure/ElecWasteCollection.Infrastructure.csproj", "ElecWasteCollection.Infrastructure/"]

# Phục hồi các phụ thuộc cho tất cả các dự án
RUN dotnet restore

# Sao chép toàn bộ mã nguồn vào trong container
COPY . .

# Build ứng dụng
WORKDIR "/src/ElecWasteCollection.API"
RUN dotnet build "ElecWasteCollection.API.csproj" -c Release -o /app/build

# Publish ứng dụng
FROM build AS publish
RUN dotnet publish "ElecWasteCollection.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - Sử dụng image nhẹ hơn để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Sao chép các tệp đã được publish từ stage trước vào container
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]
