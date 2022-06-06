FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MensattScraper/MensattScraper.csproj", "MensattScraper/"]
RUN dotnet restore "MensattScraper/MensattScraper.csproj"
COPY . .
WORKDIR "/src/MensattScraper"
RUN dotnet build "MensattScraper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MensattScraper.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MensattScraper.dll"]
