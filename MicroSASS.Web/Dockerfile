FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY ["MicroSASS.Web.csproj", "."]
RUN dotnet restore "./MicroSASS.Web.csproj"
COPY . .
RUN dotnet publish "./MicroSASS.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
RUN rm -rf ./*
COPY --from=build /app/publish/wwwroot .
EXPOSE 80
ENTRYPOINT ["nginx", "-g", "daemon off;"]