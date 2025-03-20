# Usar a imagem base do .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build da aplicação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SimpleFeed.Web.csproj", "./"]
RUN dotnet restore "./SimpleFeed.Web.csproj"

# Copiar todo o código do projeto 
COPY . .
WORKDIR "/src"

# Publicar a aplicação
RUN dotnet publish "SimpleFeed.Web.csproj" -c Release -o /app/publish

# Criar a imagem final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SimpleFeed.Web.dll"]
