FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
RUN apt-get update
RUN apt-get -y install libopus0
EXPOSE 80
#EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Autex.Backend.csproj", "./"]
RUN dotnet restore "Autex.Backend.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "Autex.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Autex.Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN ln -s /usr/lib/x86_64-linux-gnu/libopus.so.0 /app/opus.so
ENTRYPOINT ["dotnet", "Autex.Backend.dll"]