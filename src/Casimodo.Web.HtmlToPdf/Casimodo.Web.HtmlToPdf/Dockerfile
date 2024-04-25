FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
# Azure will communicate with your container internally only on port 80 (not 443).
# Azure will then accept connections on both 80 and 443 by default and forward them to port 80 internally.
EXPOSE 80
EXPOSE 443

# Install Microsoft's Ubuntu packages. 
# Install powershell for playwright installation scripts.
# Source: https://github.com/microsoft/playwright-dotnet/issues/2058
# For packages-microsoft-prod see: https://learn.microsoft.com/en-us/linux/packages
# TODO: Can we use Jammy 22.04 instead? (see https://github.com/microsoft/playwright/issues/13738)
# TODO: Can we specify that we want amd64 packages only?
RUN apt-get update -yq \
    && apt-get install wget -yq \
    && wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update -yq \
    && apt-get install powershell -yq
# Install Segoe UI; I like Segoe UI.
# TODO: Find a better place to download this from. This could be a security risk.
# Hey, Wlads and Xings: this an open invitation for you destructive Russian/Chinese fuckdips to look into our underwear and jerk a bit.
# All the glory to the assholes :-(
RUN echo "Installing Segoe UI font..."
WORKDIR /usr/share/fonts/Microsoft/TrueType/SegoeUI
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/segoeuib.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/segoeuii.ttf?raw=true 
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/segoeuiz.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/segoeuil.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguili.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/segoeuisl.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguisli.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguisb.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguisbi.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguibl.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguibli.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguiemj.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguisym.ttf?raw=true
RUN wget -q https://github.com/mrbvrz/segoe-ui/raw/master/font/seguihis.ttf?raw=true

# TODO: https://learn.microsoft.com/de-de/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-7.0
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS publish
# ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy project and restore
COPY ["Casimodo.Web.HtmlToPdf/Casimodo.Web.HtmlToPdf.csproj", "Casimodo.Web.HtmlToPdf/"]
RUN dotnet restore "./Casimodo.Web.HtmlToPdf/Casimodo.Web.HtmlToPdf.csproj"
# Copy everything else
COPY . .
WORKDIR "/src/Casimodo.Web.HtmlToPdf"
RUN dotnet publish "Casimodo.Web.HtmlToPdf.csproj" -c release -o /app/publish  /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# TODO: Do we need to delete the publish folder or will this folder be omitted in this layer anyway?
# Install playwright dependencies and remove wget and powershell.
# NOTE that the playwright.ps1 becomes available after the dotnet publish step - not earlier.
# TODO: Check if the correct chromium version was aleady installed and skip if yes.
RUN pwsh playwright.ps1 install chromium \
    && pwsh playwright.ps1 install-deps chromium \
    && rm -rf playwright.ps1 \
    && apt remove wget powershell -yq
# NOTE: in debug configuration the script is located in: "bin/Debug/net7.0/playwright.ps1"
# TODO: Do we want to uninstall packages-microsoft-prod.deb (only 4kb)?

ENTRYPOINT ["dotnet", "Casimodo.Web.HtmlToPdf.dll"]