# Stage 1: Build .NET WebAPI
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files first for efficient caching
COPY src/TH_ExamGen.Core/TH_ExamGen.Core.csproj src/TH_ExamGen.Core/
COPY src/TH_ExamGen.Services/TH_ExamGen.Services.csproj src/TH_ExamGen.Services/
COPY src/TH_ExamGen.WebAPI/TH_ExamGen.WebAPI.csproj src/TH_ExamGen.WebAPI/

# Restore dependencies
RUN dotnet restore src/TH_ExamGen.WebAPI/TH_ExamGen.WebAPI.csproj

# Copy the rest of the source code
COPY src/TH_ExamGen.Core/ src/TH_ExamGen.Core/
COPY src/TH_ExamGen.Services/ src/TH_ExamGen.Services/
COPY src/TH_ExamGen.WebAPI/ src/TH_ExamGen.WebAPI/

# Publish WebAPI
RUN dotnet publish src/TH_ExamGen.WebAPI/TH_ExamGen.WebAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install native dependencies for fonts / SkiaSharp
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    fontconfig \
    fonts-dejavu-core \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render dynamically injects $PORT environment variable
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TH_ExamGen.WebAPI.dll"]
