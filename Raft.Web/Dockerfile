FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY ["Raft.Web/Raft.Web.csproj", "Raft.Web/"]
RUN dotnet restore "Raft.Web/Raft.Web.csproj"
COPY . .
WORKDIR /source/Raft.Web
RUN dotnet publish -c Release -o /app

FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html
RUN rm -rf ./*
COPY --from=build /app/wwwroot .
COPY nginx.conf /etc/nginx/nginx.conf