# API .NET 8 (T-13.1). Contexto de build: backend/. Execução sem root (usuário "app" da imagem aspnet, uid 1654).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/Joya.Pedidos.Core/Joya.Pedidos.Core.csproj src/Joya.Pedidos.Core/
COPY src/Joya.Pedidos.Infrastructure/Joya.Pedidos.Infrastructure.csproj src/Joya.Pedidos.Infrastructure/
COPY src/Joya.Pedidos.Api/Joya.Pedidos.Api.csproj src/Joya.Pedidos.Api/
RUN dotnet restore src/Joya.Pedidos.Api/Joya.Pedidos.Api.csproj
COPY src/ src/
RUN dotnet publish src/Joya.Pedidos.Api/Joya.Pedidos.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
# A imagem aspnet não traz curl nem wget: sonda HTTP mínima com bash (/dev/tcp) e grep.
HEALTHCHECK --interval=5s --timeout=5s --retries=20 --start-period=10s \
  CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080 && printf "GET /health/ready HTTP/1.0\r\nHost: localhost\r\n\r\n" >&3 && head -n1 <&3 | grep -q " 200 "' || exit 1
ENTRYPOINT ["dotnet", "Joya.Pedidos.Api.dll"]
