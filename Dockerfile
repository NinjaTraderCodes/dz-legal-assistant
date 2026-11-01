# Web demo of DZ Legal Assistant (src/DzLegal.Web). Build from the repository root:
#   docker build -t dzlegal-web . && docker run -p 7860:7860 -e OPENROUTER_API_KEY=... dzlegal-web
# The desktop app is not part of this image. See docs/WEB-DEMO.md.

# The public website (site/, Astro) is built first, on its own; its static output joins the app under /app/site.
# Only the sources the site's generators read are copied here, so a change elsewhere does not rebuild it.
# NODE_IMAGE lets a machine that cannot reach Docker Hub build through a mirror of the same official image.
ARG NODE_IMAGE=node:22-alpine
FROM ${NODE_IMAGE} AS site
WORKDIR /repo/site
COPY site/package.json site/package-lock.json ./
RUN npm ci --omit=dev --no-audit --no-fund
COPY src/DzLegal.App/App.axaml /repo/src/DzLegal.App/App.axaml
COPY src/DzLegal.App/Styles/Workbench.axaml /repo/src/DzLegal.App/Styles/Workbench.axaml
COPY src/DzLegal.App/Assets/Images/ /repo/src/DzLegal.App/Assets/Images/
COPY src/DzLegal.Presentation/Resources/Strings.resx src/DzLegal.Presentation/Resources/Strings.en.resx /repo/src/DzLegal.Presentation/Resources/
COPY src/DzLegal.Web/wwwroot/ /repo/src/DzLegal.Web/wwwroot/
COPY assets/fonts/ /repo/assets/fonts/
COPY data/law/codes/ /repo/data/law/codes/
COPY data/law/claims/ /repo/data/law/claims/
COPY data/law/sources.json /repo/data/law/sources.json
COPY Directory.Build.props /repo/Directory.Build.props
COPY site/ ./
RUN npm run build

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
COPY --from=site /repo/site/dist ./site
# Port 7860 (Render: set PORT=7860; Cloud Run: --port 7860) and a non-root user.
# Workstation GC keeps memory low on a 512 MB instance; the server has one small CPU anyway.
ENV ASPNETCORE_HTTP_PORTS=7860 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_gcServer=0 DOTNET_GCConserveMemory=5
EXPOSE 7860
USER 1000
ENTRYPOINT ["dotnet", "DzLegal.Web.dll"]
