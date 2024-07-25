FROM mcr.microsoft.com/playwright/python:v1.45.0-jammy

ENV GITHUB_RUNNER_LABELS="playwright-1.x,dotnet"
ENV GITHUB_RUNNER_GROUP="Default"
ENV GITHUB_RUNNER_WORKDIR="_work"

RUN DEBIAN_FRONTEND=noninteractive apt-get update
RUN DEBIAN_FRONTEND=noninteractive apt-get upgrade -y

RUN DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends \
    apt-transport-https apt-utils ca-certificates \
    curl git iputils-ping jq lsb-release openssh-client \
    software-properties-common

# Install the .NET SDK 8.0
RUN apt-get install wget
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN rm packages-microsoft-prod.deb
RUN apt-get update && apt-get install -y dotnet-sdk-8.0

# Install Playwright
RUN dotnet tool install --global Microsoft.Playwright.CLI --version 1.2.3

# Can be 'linux-x64', 'linux-arm64', 'linux-arm', 'rhel.6-x64'.
ENV TARGETARCH=linux-x64

# Actions Runner
WORKDIR /acr

# Create a new user for the runner
RUN useradd -m acru1 && \
    chown -R acru1:acru1 /acr

# Download the latest runner package
RUN curl -o actions-runner-linux-x64-2.317.0.tar.gz \
  -L https://github.com/actions/runner/releases/download/v2.317.0/actions-runner-linux-x64-2.317.0.tar.gz
RUN tar xzf ./actions-runner-linux-x64-2.317.0.tar.gz

# Run a self-hosted Azure Pipelines agent in Docker
COPY ./start.sh .
RUN chmod +x start.sh

# Switch to the acru1 user
USER acru1

ENTRYPOINT [ "./start.sh" ]