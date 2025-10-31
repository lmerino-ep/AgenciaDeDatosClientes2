# Multi-stage build optimizado para producción ECS/Fargate
# Fase base con runtime optimizado
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app

# Instalar dependencias del sistema, ImageMagick y crear usuario no-root
RUN apk add --no-cache \
    curl \
    icu-libs \
    icu-data-full \
    tzdata \
    imagemagick \
    imagemagick-dev \
    libgdiplus \
    && addgroup -g 1001 -S appgroup \
    && adduser -u 1001 -S appuser -G appgroup \
    && mkdir -p /app/logs

# Fase de preparación de archivos compilados
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS prepare
WORKDIR /src

# Copiar todo el directorio bin para asegurar que existe
COPY ["bin/", "./bin/"]

# Verificar que los archivos se copiaron correctamente
RUN ls -la /src/bin/Release/net9.0/Dashboard.dll

# Fase de optimización y limpieza
FROM prepare AS optimize
WORKDIR /app

# Copiar archivos desde la fase prepare (específicamente desde /src/bin/Release/net9.0)
COPY --from=prepare /src/bin/Release/net9.0/* ./

# Verificar archivos antes de limpieza
RUN ls -la /app/Dashboard.dll

# Eliminar archivos innecesarios para reducir tamaño (pero mantener archivos nativos de Linux)
RUN rm -f *.pdb \
    && rm -f *.xml \
    && rm -f *.Development.json \
    && rm -f *.Local.json \
    && rm -rf runtimes/win* \
    && rm -rf runtimes/osx* \
    && rm -rf runtimes/linux-arm64 \
    && ls -la /app/Dashboard.dll

# Fase final ultra-optimizada
FROM base AS final
WORKDIR /app

# Copiar archivos optimizados
COPY --from=optimize /app .

# Configurar permisos ANTES de cambiar al usuario no-root
RUN chown -R appuser:appgroup /app \
    && chmod +x /app/Dashboard.dll

# Cambiar al usuario no-root
USER appuser

# Exponer puerto para health checks
EXPOSE 8080

# Configurar variables de entorno para optimización de producción
ENV DOTNET_EnableDiagnostics=0 \
    DOTNET_GCAllowVeryLargeObjects=1 \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    MAGICK_HOME=/usr \
    MAGICK_CONFIGURE_PATH=/etc/ImageMagick-7

# Verificar que ImageMagick está instalado correctamente (como root antes del USER)
USER root
RUN magick -version && \
    convert -version
USER appuser

# Health check optimizado
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/healthcheck || exit 1

# Comando de entrada optimizado
ENTRYPOINT ["dotnet", "Dashboard.dll"]