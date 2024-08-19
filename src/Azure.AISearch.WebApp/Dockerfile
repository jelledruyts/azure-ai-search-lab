FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG app_version=1.0.0.0
ARG source_version=local
WORKDIR /build
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app /p:Version=${app_version} /p:SourceRevisionId=${source_version}

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Azure.AISearch.WebApp.dll"]