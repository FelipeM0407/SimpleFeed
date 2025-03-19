# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia os arquivos do projeto e restaura as dependências
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Define a porta padrão da aplicação
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Comando de inicialização
CMD ["dotnet", "SimpleFeed.Web.dll"]
