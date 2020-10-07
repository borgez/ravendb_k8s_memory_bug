FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster as build

WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

ADD . .

CMD ["dotnet", "run"]