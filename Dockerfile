FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["PurchasesBot/PurchasesBot.csproj", "PurchasesBot/"]
RUN dotnet restore "PurchasesBot/PurchasesBot.csproj"
COPY . .
WORKDIR "/src/PurchasesBot"
RUN dotnet publish "PurchasesBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PurchasesBot.dll"]

