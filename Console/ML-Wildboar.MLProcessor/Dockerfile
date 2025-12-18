# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Console/ML-Wildboar.MLProcessor/ML-Wildboar.MLProcessor.csproj", "Console/ML-Wildboar.MLProcessor/"]
COPY ["Shared/ML-Wildboar.Shared.Core/ML-Wildboar.Shared.Core.csproj", "Shared/ML-Wildboar.Shared.Core/"]
COPY ["Shared/ML-Wildboar.Shared.Storage/ML-Wildboar.Shared.Storage.csproj", "Shared/ML-Wildboar.Shared.Storage/"]

# Restore dependencies
RUN dotnet restore "Console/ML-Wildboar.MLProcessor/ML-Wildboar.MLProcessor.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/Console/ML-Wildboar.MLProcessor"
RUN dotnet build "ML-Wildboar.MLProcessor.csproj" -c Release -o /app/build
RUN dotnet publish "ML-Wildboar.MLProcessor.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ML-Wildboar.MLProcessor.dll"]
