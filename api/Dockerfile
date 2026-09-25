FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY *.slnx .
COPY src/Todo.Api/*.csproj src/Todo.Api/
RUN dotnet restore src/Todo.Api/Todo.Api.csproj
COPY src/Todo.Api/ src/Todo.Api/
RUN dotnet publish src/Todo.Api/Todo.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
RUN adduser -D -u 10001 appuser
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
USER 10001
EXPOSE 8080
ENTRYPOINT ["dotnet", "Todo.Api.dll"]