# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . .

RUN dotnet publish BibliaStudy.Api/BibliaStudy.Api.csproj -c Release -o out

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out .

EXPOSE 10000

ENTRYPOINT ["dotnet", "BibliaStudy.Api.dll"]