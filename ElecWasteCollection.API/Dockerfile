# ================== BUILD =====================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ElecWasteCollection.API/*.csproj ./ElecWasteCollection.API/
COPY ElecWasteCollection.Application/*.csproj ./ElecWasteCollection.Application/
COPY ElecWasteCollection.Domain/*.csproj ./ElecWasteCollection.Domain/
COPY ElecWasteCollection.Infrastructure/*.csproj ./ElecWasteCollection.Infrastructure/
RUN dotnet restore ElecWasteCollection.API/ElecWasteCollection.API.csproj
COPY . .
WORKDIR /src/ElecWasteCollection.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ================== RUNTIME =====================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# hoặc dùng: FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime

RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libgl1-mesa-glx \
    libglib2.0-0 \
    libgtk2.0-0 \
    libxext6 \
    libxrender1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ElecWasteCollection.API.dll"]