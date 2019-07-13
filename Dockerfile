FROM microsoft/dotnet:2.2-sdk AS installer-env

COPY ./splunk-aks-backup-func /src/dotnet-function-app
RUN cd /src/dotnet-function-app && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot
COPY ./scripts /home/scripts

FROM mcr.microsoft.com/azure-functions/dotnet:2.0

COPY --from=installer-env ["/home/site", "/home/site"]

RUN apt-get update && \
    apt-get install -y curl

# install AZ CLI
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash

# install kubectl
RUN cd ~ && \
    curl -LO https://storage.googleapis.com/kubernetes-release/release/$(curl -s https://storage.googleapis.com/kubernetes-release/release/stable.txt)/bin/linux/amd64/kubectl && \
    chmod +x ./kubectl && \
    mv ./kubectl /usr/local/bin/kubectl

# install powershell
RUN cd ~ && \
    apt-get update && \
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-stretch-prod stretch main" > /etc/apt/sources.list.d/microsoft.list' && \
    apt-get update && \
    apt-get install -y powershell

# install Az
RUN pwsh -Command "& {Install-Module -Name Az -AllowClobber -Force}"

# install git
RUN apt-get install -y git

