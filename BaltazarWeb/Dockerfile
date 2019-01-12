FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["BaltazarWeb/BaltazarWeb.csproj", "BaltazarWeb/"]
RUN dotnet restore "BaltazarWeb/BaltazarWeb.csproj"
COPY . .
WORKDIR "/src/BaltazarWeb"
RUN dotnet build "BaltazarWeb.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "BaltazarWeb.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "BaltazarWeb.dll"]