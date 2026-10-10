# Web demo of DZ Legal Assistant (src/DzLegal.Web). Build from the repository root:
#   docker build -t dzlegal-web . && docker run -p 7860:7860 -e GOOGLE_API_KEY=... dzlegal-web
# The desktop app is not part of this image. See docs/WEB-DEMO.md.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
COPY . .
# Ready-to-run code starts faster on a small shared CPU (the free host wakes the service on the first visit).
RUN dotnet publish src/DzLegal.Web -c Release -o /app -r linux-x64 --self-contained false -p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# QuestPDF (the PDF engine) needs fontconfig on Linux; the Arabic font itself is embedded.
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# Port 7860 and user 1000 suit both Cloud Run (deploy/cloud-run.sh passes --port 7860) and Hugging Face Spaces.
# Workstation GC keeps memory low on a 512 MB instance; the server has one small CPU anyway.
ENV ASPNETCORE_HTTP_PORTS=7860 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_gcServer=0 DOTNET_GCConserveMemory=5
EXPOSE 7860
USER 1000
ENTRYPOINT ["dotnet", "DzLegal.Web.dll"]
