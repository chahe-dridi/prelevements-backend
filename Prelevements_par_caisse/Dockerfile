# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first to leverage caching
COPY ["Prelevements_par_caisse/Prelevements_par_caisse.csproj", "Prelevements_par_caisse/"]
RUN dotnet restore "Prelevements_par_caisse/Prelevements_par_caisse.csproj"

# Copy the rest of the files
COPY . .

WORKDIR "/src/Prelevements_par_caisse"
RUN dotnet build "Prelevements_par_caisse.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Prelevements_par_caisse.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Prelevements_par_caisse.dll"]
