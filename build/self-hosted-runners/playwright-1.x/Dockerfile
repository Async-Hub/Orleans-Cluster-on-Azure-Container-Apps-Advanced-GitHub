# The image is based on Ubuntu 24.04
FROM mcr.microsoft.com/playwright/python:v1.61.0-noble

ENV TARGETARCH=linux-x64
ENV GITHUB_RUNNER_LABELS="playwright-1.x,dotnet"
ENV GITHUB_RUNNER_WORKDIR="_work"

RUN DEBIAN_FRONTEND=noninteractive apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get upgrade -y && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends \
    apt-transport-https apt-utils ca-certificates \
    curl git iputils-ping jq lsb-release openssh-client \
    software-properties-common

# Install the .NET SDK 10.0
RUN add-apt-repository ppa:dotnet/backports && \
    apt-get update && apt-get install -y dotnet-sdk-10.0

# Actions Runner
WORKDIR /acr

# Create u1 user and set up home directory
RUN useradd -m -d /home/u1 u1

# Download the latest runner package
RUN curl -o actions-runner-linux-x64-2.337.0.tar.gz \
    -L https://github.com/actions/runner/releases/download/v2.337.0/actions-runner-linux-x64-2.337.0.tar.gz && \
    tar xzf ./actions-runner-linux-x64-2.337.0.tar.gz

# Run a self-hosted GitHub Actions runner in Docker
COPY ./start.sh .
RUN chmod +x start.sh

# Ensure u1 owns everything (runner files were extracted as root) before dropping privileges
RUN chown -R u1:u1 /acr /home/u1
USER u1

ENTRYPOINT [ "./start.sh" ]