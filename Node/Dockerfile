FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["Node/Node.csproj", "Node/"]
RUN dotnet restore "Node/Node.csproj"

COPY . .
WORKDIR /source/Node
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Node.dll"]