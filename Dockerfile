# Backend .NET 10 no padrao Univali (DEPLOY_STACK.md: OracleLinux 10 nos dois estagios).
# A imagem NAO embute o frontend — o front Expo tem container proprio (frontend/).

# ---------- build ----------
FROM oraclelinux:10 AS build
WORKDIR /src
ENV DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    PATH=/usr/share/dotnet:${PATH}

RUN dnf install -y --setopt=install_weak_deps=False \
        curl ca-certificates libicu libstdc++ zlib krb5-libs openssl-libs \
    && dnf clean all \
    && curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh \
    && ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

COPY DocBr.Validacao/DocBr.Validacao.csproj DocBr.Validacao/
COPY DocBr.Ocr/DocBr.Ocr.csproj DocBr.Ocr/
COPY Api.Host/Api.Host.csproj Api.Host/
RUN dotnet restore Api.Host/Api.Host.csproj

COPY . .
RUN dotnet publish Api.Host/Api.Host.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM oraclelinux:10 AS final
ENV DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    ASPNETCORE_URLS=http://+:8080 \
    TZ=America/Sao_Paulo \
    PATH=/usr/share/dotnet:${PATH}

# tesseract/leptonica sao dependencias NATIVAS do OCR: o pacote NuGet Tesseract so traz o
# binario de Windows, entao no Linux a biblioteca do sistema e obrigatoria. fontconfig e
# freetype vem por causa do SkiaSharp. EPEL porque o tesseract nao esta nos repos base do OL10.
RUN dnf install -y --setopt=install_weak_deps=False \
        oracle-epel-release-el10 \
    && dnf install -y --setopt=install_weak_deps=False \
        curl ca-certificates libicu libstdc++ zlib krb5-libs openssl-libs \
        tesseract leptonica fontconfig freetype \
    && dnf clean all \
    && curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh \
    && ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet \
    && useradd -r -u 1001 -m appuser

WORKDIR /app
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.Host.dll"]
