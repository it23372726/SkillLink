# =========================
# 1) Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY SkillLink.API.csproj ./
RUN dotnet restore

# Copy rest of the source and publish
COPY . .
RUN dotnet publish "SkillLink.API.csproj" -c Release -o /out

# =========================
# 2) Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /out ./

# Kestrel inside container will listen on 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SkillLink.API.dll"]
