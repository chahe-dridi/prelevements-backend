FROM jenkins/jenkins:lts

USER root

# Install dependencies
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    curl \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*


# Install .NET SDK 8
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 8.0 \
    && ln -s /root/.dotnet/dotnet /usr/bin/dotnet

# Set environment variables
ENV DOTNET_ROOT=/root/.dotnet
ENV PATH=$PATH:/root/.dotnet:/root/.dotnet/tools

# Install SonarScanner for .NET
RUN /root/.dotnet/dotnet tool install --global dotnet-sonarscanner

# Ensure tools are accessible to Jenkins user too
RUN echo 'export PATH=$PATH:/root/.dotnet:/root/.dotnet/tools' >> /etc/profile

USER jenkins
