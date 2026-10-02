FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src


COPY SoundEffectLibrary.Api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release --no-restore -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SoundEffectLibrary.Api.dll"]