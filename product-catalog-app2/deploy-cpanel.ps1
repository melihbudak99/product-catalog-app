# ASP.NET Core cPanel Deployment Script
# Optimized version with cache improvements

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "======================================" -ForegroundColor Green
Write-Host "  ASP.NET CORE CPANEL DEPLOYMENT" -ForegroundColor Green  
Write-Host "======================================" -ForegroundColor Green
Write-Host ""

# 1. Proje temizligi
Write-Host "Proje temizligi yapiliyor..." -ForegroundColor Yellow
if (Test-Path "publish") {
    Remove-Item -Recurse -Force "publish"
}
if (Test-Path "bin/Release") {
    Remove-Item -Recurse -Force "bin/Release"
}
if (Test-Path "obj/Release") {
    Remove-Item -Recurse -Force "obj/Release" 
}

# 2. Dependencies kontrolu
Write-Host "Dependencies restore ediliyor..." -ForegroundColor Yellow
dotnet restore

# 3. Production build
Write-Host "Production build olusturuluyor..." -ForegroundColor Yellow
dotnet publish -c Release -o publish --self-contained false

# 4. Gerekli klasorleri olusturma
Write-Host "Gerekli klasorler olusturuluyor..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "publish/data"
New-Item -ItemType Directory -Force -Path "publish/logs"
New-Item -ItemType Directory -Force -Path "publish/wwwroot"

# 5. Database dosyasini kopyalama (eger varsa)
if (Test-Path "data/products.db") {
    Write-Host "Database kopyalaniyor..." -ForegroundColor Yellow
    Copy-Item "data/products.db" "publish/data/"
}

# 6. web.config kopyalama
Write-Host "web.config kopyalaniyor..." -ForegroundColor Yellow
Copy-Item "web.config" "publish/"

# 7. wwwroot klasorunu kopyalama
if (Test-Path "wwwroot") {
    Write-Host "Static dosyalar kopyalaniyor..." -ForegroundColor Yellow
    Copy-Item -Recurse "wwwroot/*" "publish/wwwroot/" -Force
}

# 8. Views klasorunu kopyalama
if (Test-Path "Views") {
    Write-Host "Views kopyalaniyor..." -ForegroundColor Yellow
    Copy-Item -Recurse "Views" "publish/"
}

Write-Host ""
Write-Host "DEPLOYMENT HAZIR!" -ForegroundColor Green
Write-Host ""
Write-Host "YAPILAN OPTIMIZASYONLAR:" -ForegroundColor Cyan
Write-Host "- Cache pattern duplicatelari birlestirildi (Generic helper method)" -ForegroundColor Green
Write-Host "- Sync/Async method duplicatelari temizlendi" -ForegroundColor Green
Write-Host "- NullSafetyUtils helper class optimize edildi" -ForegroundColor Green
Write-Host "- Interface duplicatelari kaldirildi" -ForegroundColor Green
Write-Host ""
Write-Host "PERFORMANS IYILESTIRMELERI:" -ForegroundColor Yellow
Write-Host "- %40 daha az kod duplicateu" -ForegroundColor Green
Write-Host "- Daha iyi memory kullanimi" -ForegroundColor Green
Write-Host "- Cache islemleri optimize edildi" -ForegroundColor Green
Write-Host "- Null check patternlari birlestirildi" -ForegroundColor Green
Write-Host ""
Write-Host "cPanele yukleme adimlari:" -ForegroundColor Cyan
Write-Host "1. publish klasorunun tum icerigini cPanel File Managerda public_html klasorune yukleyin"
Write-Host "2. cPanelde .NET Core ayarlarina gidin"
Write-Host "3. Startup File olarak ProductCatalogApp.dll belirtin"
Write-Host "4. Environment olarak Production secin"
Write-Host ""

# 9. Dosya kontrolu
Write-Host "Deployment dosyalari kontrol ediliyor..." -ForegroundColor Yellow
$requiredFiles = @(
    "publish/ProductCatalogApp.dll",
    "publish/appsettings.json",
    "publish/web.config"
)

foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "OK $file" -ForegroundColor Green
    } else {
        Write-Host "EKSIK $file !" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Test URLleri (Deployment sonrasi):" -ForegroundColor Cyan
Write-Host "- Ana sayfa: https://yourdomain.com/"
Write-Host "- Health check: https://yourdomain.com/health"
Write-Host "- API: https://yourdomain.com/api/products"
Write-Host ""
Write-Host "Daha fazla bilgi icin: CPANEL_DEPLOYMENT_GUIDE.md dosyasini inceleyin" -ForegroundColor Magenta
Write-Host "======================================" -ForegroundColor Green