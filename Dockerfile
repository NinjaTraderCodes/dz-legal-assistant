# Web demo of DZ Legal Assistant (src/DzLegal.Web). Build from the repository root:
#   docker build -t dzlegal-web . && docker run -p 7860:7860 -e GOOGLE_API_KEY=... dzlegal-web
# The desktop app is not part of this image. See docs/WEB-DEMO.md.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
COPY . .
RUN dotnet publish src/DzLegal.Web -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# QuestPDF (the PDF engine) needs fontconfig on Linux; the Arabic font itself is embedded.
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# Hugging Face Spaces route to port 7860 and run the container as user 1000.
ENV ASPNETCORE_HTTP_PORTS=7860 DOTNET_CLI_TELEMETRY_OPTOUT=1
EXPOSE 7860
USER 1000
ENTRYPOINT ["dotnet", "DzLegal.Web.dll"]
