FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["KulcsRendszer.slnx", "./"]
COPY ["KulcsRendszer.API/KulcsRendszer.API.csproj", "KulcsRendszer.API/"]
COPY ["KulcsRendszer.DataContext/KulcsRendszer.DataContext.csproj", "KulcsRendszer.DataContext/"]
COPY ["KulcsRendszer.Services/KulcsRendszer.Services.csproj", "KulcsRendszer.Services/"]

RUN dotnet restore "KulcsRendszer.slnx"

COPY . .
RUN dotnet publish "KulcsRendszer.API/KulcsRendszer.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KulcsRendszer.API.dll"]