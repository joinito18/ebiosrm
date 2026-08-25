FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY EbiosRM.Api.csproj .
RUN dotnet restore EbiosRM.Api.csproj
COPY . .
RUN dotnet publish EbiosRM.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "EbiosRM.Api.dll"]
