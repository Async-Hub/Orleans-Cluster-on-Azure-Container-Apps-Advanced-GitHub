FROM ubuntu:24.04

ENV TARGETARCH=linux-x64
ENV GITHUB_RUNNER_LABELS="azure-cli,dotnet,resharper-cli"
ENV GITHUB_RUNNER_WORKDIR="_work"

RUN DEBIAN_FRONTEND=noninteractive apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get upgrade -y && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends \
    apt-transport-https \
    apt-utils \
    ca-certificates \
    curl \
    git \
    iputils-ping \
    jq \
    gnupg \
    lsb-release \
    software-properties-common

# Install the Azure CLI and Bicep CLI
# RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash
WORKDIR /azcli
COPY ./az_cli_install.sh .
RUN chmod +x az_cli_install.sh && ./az_cli_install.sh && \
    curl -fL -o /usr/local/bin/bicep https://github.com/Azure/bicep/releases/latest/download/bicep-linux-x64 && \
    chmod +x /usr/local/bin/bicep
ENV AZURE_CLI=True

# Install the .NET SDK 10.0
RUN apt-get install wget && \
  wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && apt-get install -y dotnet-sdk-10.0

# https://blog.jetbrains.com/dotnet/2020/07/16/resharper-command-line-tools-cross-platform-and-global-tools/#usage-as-global-tools
RUN dotnet tool install JetBrains.ReSharper.GlobalTools --tool-path /resharpercli --version 2026.2.0
ENV JETBRAINS_RESHARPER_GLOBALTOOLS=2026.2.0

# Install PowerShell
RUN apt-get install -y powershell

# Install Docker Engine on Ubuntu
# Use Docker within a Docker container
# Warning!!! Doing this has serious security implications.
# The code inside the container can now run as root on your Docker host.
RUN curl -fsSL https://download.docker.com/linux/ubuntu/gpg | apt-key add - \
  && add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu focal stable" \
  && apt-cache policy docker-ce \
  && apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Actions Runner
WORKDIR /acr

# Download the latest runner package
RUN curl -o actions-runner-linux-x64-2.337.0.tar.gz \
    -L https://github.com/actions/runner/releases/download/v2.337.0/actions-runner-linux-x64-2.337.0.tar.gz && \
    tar xzf ./actions-runner-linux-x64-2.337.0.tar.gz

# Run a self-hosted GitHub Actions runner in Docker
COPY ./start.sh .
RUN chmod +x start.sh

# Create u1 user and set up home directory
RUN useradd -m -d /home/u1 u1 \
    && chown -R u1:u1 /acr /home/u1 \
    && groupadd -f docker \
    && usermod -aG docker u1

USER u1

ENTRYPOINT [ "./start.sh" ]
