# Docker Debug Deployment Script
# Bu script projeyi debug mode ile Docker'da calistir

param(
    [switch]$Rebuild,
    [switch]$Stop,
    [switch]$Clean,
    [switch]$Follow
)

# Renkler
$Red = 'Red'
$Green = 'Green'
$Yellow = 'Yellow'
$Blue = 'Blue'
$Cyan = 'Cyan'
$Magenta = 'Magenta'
$White = 'White'

Write-Host "Docker DEBUG Deployment" -ForegroundColor $Cyan
Write-Host "=======================" -ForegroundColor $Cyan

# Stop parametresi kontrolu
if ($Stop) {
    Write-Host "Stopping all containers..." -ForegroundColor $Yellow
    docker-compose -f docker-compose.debug.yml down
    exit 0
}

# Clean parametresi kontrolu
if ($Clean) {
    Write-Host "Cleaning Docker resources..." -ForegroundColor $Yellow
    docker-compose -f docker-compose.debug.yml down -v --remove-orphans
    docker system prune -f
    exit 0
}

# Docker kontrolu
try {
    $dockerVersion = docker version --format "{{.Server.Version}}" 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
    Write-Host "SUCCESS: Docker is running (v$dockerVersion)" -ForegroundColor $Green
} catch {
    Write-Host "ERROR: Docker is not running or not installed" -ForegroundColor $Red
    Write-Host "Please start Docker Desktop and try again." -ForegroundColor $Yellow
    exit 1
}

# Database dosyasi kontrolu
if (-not (Test-Path "data/products.db")) {
    Write-Host "Creating data directory..." -ForegroundColor $Yellow
    New-Item -ItemType Directory -Force -Path "data" | Out-Null
    
    if (Test-Path "products.db") {
        Write-Host "Copying database to data directory..." -ForegroundColor $Yellow
        Copy-Item "products.db" "data/products.db"
        Write-Host "SUCCESS: Database copied successfully" -ForegroundColor $Green
    } else {
        Write-Host "WARNING: No existing database found. App will create a new one." -ForegroundColor $Yellow
    }
}

# Onceki containerlari temizle
Write-Host "Cleaning up previous containers..." -ForegroundColor $Yellow
docker-compose -f docker-compose.debug.yml down 2>$null

# Rebuild parametresi kontrolu
if ($Rebuild) {
    Write-Host "Rebuilding Docker images for DEBUG..." -ForegroundColor $Yellow
    docker-compose -f docker-compose.debug.yml build --no-cache
}

# Containerlari baslat
Write-Host "Starting containers in DEBUG mode..." -ForegroundColor $Green
docker-compose -f docker-compose.debug.yml up -d --build

# Container durumlarini kontrol et
Start-Sleep -Seconds 3
$containers = docker-compose -f docker-compose.debug.yml ps --format "table {{.Name}}\t{{.Status}}"
Write-Host ""
Write-Host "Container Status:" -ForegroundColor $Cyan
Write-Host $containers

Write-Host ""
Write-Host "SUCCESS: Debug environment is ready!" -ForegroundColor $Green
Write-Host ""
Write-Host "DEBUG URLS:" -ForegroundColor $Cyan
Write-Host "   Application: http://localhost:5000" -ForegroundColor $Blue
Write-Host ""
Write-Host "DEBUG COMMANDS:" -ForegroundColor $Cyan
Write-Host "   Real-time logs:  .\deploy-docker-debug.ps1 -Follow" -ForegroundColor $White
Write-Host "   View logs:       docker-compose -f docker-compose.debug.yml logs product-catalog" -ForegroundColor $White
Write-Host "   Follow logs:     docker-compose -f docker-compose.debug.yml logs -f product-catalog" -ForegroundColor $White
Write-Host "   Container shell: docker exec -it product-catalog-debug bash" -ForegroundColor $White
Write-Host "   Stop debug:      .\deploy-docker-debug.ps1 -Stop" -ForegroundColor $White

# Follow parametresi kontrolu
if ($Follow) {
    Write-Host ""
    Write-Host "Following real-time logs (Ctrl+C to exit)..." -ForegroundColor $Yellow
    docker-compose -f docker-compose.debug.yml logs -f product-catalog
}
