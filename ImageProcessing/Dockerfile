# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY ["ImageProcessing/ML-Wildboar.ImageProcessor.csproj", "ImageProcessing/"]
COPY ["Shared/ML-Wildboar.Shared.Core/ML-Wildboar.Shared.Core.csproj", "Shared/ML-Wildboar.Shared.Core/"]
COPY ["Shared/ML-Wildboar.Shared.Storage/ML-Wildboar.Shared.Storage.csproj", "Shared/ML-Wildboar.Shared.Storage/"]

# Restore dependencies
RUN dotnet restore "ImageProcessing/ML-Wildboar.ImageProcessor.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/ImageProcessing"
RUN dotnet build "ML-Wildboar.ImageProcessor.csproj" -c Release -o /app/build
RUN dotnet publish "ML-Wildboar.ImageProcessor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ML-Wildboar.ImageProcessor.dll"]
