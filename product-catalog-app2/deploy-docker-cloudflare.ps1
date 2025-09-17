# Docker + Cloudflare Tunnel Deployment Script
# Bu script projeyi Docker ile baslatir ve Cloudflare tunnel ile erisilebilir yapar

param(
    [switch]$Rebuild,
    [switch]$Stop,
    [switch]$Clean
)

# Renkler
$Red = 'Red'
$Green = 'Green'
$Yellow = 'Yellow'
$Blue = 'Blue'
$Cyan = 'Cyan'
$Magenta = 'Magenta'
$White = 'White'

Write-Host "Docker + Cloudflare Tunnel Deployment" -ForegroundColor $Cyan
Write-Host "=====================================" -ForegroundColor $Cyan

# Stop parametresi kontrolu
if ($Stop) {
    Write-Host "Stopping all containers..." -ForegroundColor $Yellow
    docker-compose down
    exit 0
}

# Clean parametresi kontrolu
if ($Clean) {
    Write-Host "Cleaning Docker resources..." -ForegroundColor $Yellow
    docker-compose down -v --remove-orphans
    docker system prune -f
    exit 0
}

# .env dosyasi kontrolu
if (-not (Test-Path ".env")) {
    Write-Host "ERROR: .env file not found!" -ForegroundColor $Red
    Write-Host "Creating .env file..." -ForegroundColor $Yellow
    @"
CLOUDFLARE_TUNNEL_TOKEN=your_token_here
"@ | Out-File -FilePath ".env" -Encoding UTF8
    Write-Host "WARNING: Please edit .env file and add your Cloudflare tunnel token" -ForegroundColor $Yellow
    Write-Host "Then run the script again." -ForegroundColor $Yellow
    exit 1
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

# Docker Compose kontrolu
try {
    $composeVersion = docker-compose version --short 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose not found"
    }
    Write-Host "SUCCESS: Docker Compose is available (v$composeVersion)" -ForegroundColor $Green
} catch {
    Write-Host "ERROR: Docker Compose is not available" -ForegroundColor $Red
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
docker-compose down 2>$null

# Rebuild parametresi kontrolu
if ($Rebuild) {
    Write-Host "Rebuilding Docker images..." -ForegroundColor $Yellow
    docker-compose build --no-cache
}

# Containerlari baslat
Write-Host "Starting containers..." -ForegroundColor $Green
docker-compose up -d --build

# Container durumlarini kontrol et
Start-Sleep -Seconds 3
$containers = docker-compose ps --format "table {{.Name}}\t{{.Status}}"
Write-Host ""
Write-Host "Container Status:" -ForegroundColor $Cyan
Write-Host $containers

# Loglari takip et (5 saniye)
Write-Host ""
Write-Host "Recent logs:" -ForegroundColor $Cyan
docker-compose logs --tail=10

# Cloudflare tunnel URLini bul
Write-Host ""
Write-Host "Waiting for Cloudflare tunnel URL..." -ForegroundColor $Yellow
$tunnelUrl = $null
$maxAttempts = 30
$attempt = 0

while ($attempt -lt $maxAttempts -and -not $tunnelUrl) {
    $attempt++
    Start-Sleep -Seconds 2
    $logs = docker-compose logs cloudflared 2>$null
    
    if ($logs) {
        # trycloudflare.com URLini ara
        $urlMatch = $logs | Select-String "https://([a-z0-9-]+\.trycloudflare\.com)" | Select-Object -Last 1
        if ($urlMatch) {
            $tunnelUrl = $urlMatch.Matches.Groups[1].Value
            break
        }
    }
    Write-Host "." -NoNewline -ForegroundColor $Yellow
}

Write-Host ""

if ($tunnelUrl) {
    Write-Host "SUCCESS: Cloudflare tunnel is ready!" -ForegroundColor $Green
    Write-Host ""
    Write-Host "YOUR URLS:" -ForegroundColor $Cyan
    Write-Host "   Local:  http://localhost:5000" -ForegroundColor $Blue
    Write-Host "   Public: https://$tunnelUrl" -ForegroundColor $Blue
    Write-Host ""
    Write-Host "You can access your app from any computer using the public URL!" -ForegroundColor $Green
} else {
    Write-Host "WARNING: Could not find Cloudflare tunnel URL" -ForegroundColor $Yellow
    Write-Host "Check logs with: docker-compose logs cloudflared" -ForegroundColor $Yellow
    Write-Host ""
    Write-Host "Local URL: http://localhost:5000" -ForegroundColor $Blue
}

Write-Host ""
Write-Host "Useful commands:" -ForegroundColor $Cyan
Write-Host "   Stop:     .\deploy-docker-cloudflare.ps1 -Stop" -ForegroundColor $White
Write-Host "   Rebuild:  .\deploy-docker-cloudflare.ps1 -Rebuild" -ForegroundColor $White
Write-Host "   Clean:    .\deploy-docker-cloudflare.ps1 -Clean" -ForegroundColor $White
Write-Host "   Logs:     docker-compose logs -f" -ForegroundColor $White
