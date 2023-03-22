FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["AzureBlobStorageAPI/AzureBlobStorageAPI.csproj", "AzureBlobStorageAPI/"]
RUN dotnet restore "AzureBlobStorageAPI/AzureBlobStorageAPI.csproj"
COPY . .
WORKDIR "/src/AzureBlobStorageAPI"
RUN dotnet build "AzureBlobStorageAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureBlobStorageAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureBlobStorageAPI.dll"]
