# 🚀 cPanel ASP.NET Core Deployment Rehberi

## 📋 Ön Gereksinimler

### cPanel Hosting Gereksinimleri
- ✅ **ASP.NET Core 6.0** desteği
- ✅ **SQLite** veritabanı desteği  
- ✅ **File Manager** erişimi
- ✅ **.NET Core App** yönetim paneli

### Yerel Gereksinimler
- ✅ **.NET 6.0 SDK** yüklü
- ✅ **PowerShell** (Windows) veya **Terminal** (Mac/Linux)
- ✅ **FTP Client** veya **cPanel File Manager**

---

## 🎯 ADIM 1: Projeyi Hazırlama

### 1.1 Optimization'ları Kontrol Et
```bash
# Projede optimizasyonlar yapıldı:
✅ Cache pattern duplicate'ları temizlendi
✅ Sync/Async method uyumluluğu sağlandı
✅ cPanel uyumlu port konfigürasyonu
✅ Database path düzeltildi
```

### 1.2 Deployment Script Çalıştır
```powershell
# Windows PowerShell
cd "proje-klasörü"
.\deploy-cpanel.ps1
```

```bash
# Mac/Linux Terminal  
cd "proje-klasörü"
chmod +x deploy-cpanel.ps1
./deploy-cpanel.ps1
```

### 1.3 Build Kontrolü
```bash
# Manuel build (opsiyonel)
dotnet clean
dotnet restore  
dotnet build -c Release
dotnet publish -c Release -o publish --self-contained false
```

---

## 🌐 ADIM 2: cPanel Konfigürasyonu

### 2.1 cPanel'e Giriş
1. cPanel hesabınıza giriş yapın
2. **".NET Core"** veya **"App Manager"** bölümünü bulun
3. **"Create New App"** tıklayın

### 2.2 .NET Core App Oluşturma
```
Application Root: public_html/
App Type: .NET Core 6.0
Startup File: ProductCatalogApp.dll
Environment: Production
```

### 2.3 Environment Variables Ayarları
cPanel .NET Core ayarlarında bu değişkenleri ekleyin:

```env
ASPNETCORE_ENVIRONMENT=Production
ENABLE_HTTPS_REDIRECT=false
ASPNETCORE_HTTPS_PORT=443
SQLITE_DATABASE_PATH=~/data/products.db
DOTNET_gcServer=1
DOTNET_gcConcurrent=1
DOTNET_ThreadPool_MinWorkerThreads=16
DOTNET_ThreadPool_MinCompletionPortThreads=16
LOG_LEVEL=Information
ENABLE_DETAILED_ERRORS=false
ENABLE_SENSITIVE_DATA_LOGGING=false
CACHE_EXPIRATION_MINUTES=30
PERFORMANCE_LOGGING_ENABLED=true
REQUIRE_HTTPS=false
MAX_REQUEST_SIZE=10485760
SESSION_TIMEOUT=60
```

---

## 📁 ADIM 3: Dosya Yükleme

### 3.1 File Manager ile Yükleme
1. cPanel **File Manager**'ı açın
2. **public_html** klasörüne gidin
3. **publish** klasörünün tüm içeriğini seçin
4. **Upload** veya **Extract** yapın

### 3.2 FTP ile Yükleme
```bash
# FileZilla veya benzeri FTP client kullanın
Host: ftp.yourdomain.com
Username: cPanel kullanıcı adı
Password: cPanel şifresi
Port: 21

# Hedef klasör: /public_html/
```

### 3.3 Gerekli Dosya Kontrolü
Yüklenen dosyalarda bunların olduğundan emin olun:
```
✅ ProductCatalogApp.dll
✅ web.config
✅ appsettings.json
✅ appsettings.Production.json
✅ data/ klasörü
✅ wwwroot/ klasörü
✅ Views/ klasörü
```

---

## 🗄️ ADIM 4: Database Kurulumu

### 4.1 SQLite Database
Uygulama ilk çalıştığında otomatik olarak:
- ✅ `data/products.db` dosyası oluşturulur
- ✅ 25 varsayılan kategori eklenir
- ✅ Tüm tablolar ve indexler hazırlanır

### 4.2 Database Permissions
```bash
# File Manager'da izinleri kontrol edin:
data/ klasörü: 755
products.db: 644
```

### 4.3 Mevcut Database Yükleme (Opsiyonel)
Varolan bir database'iniz varsa:
1. `data/products.db` dosyasını yükleyin
2. File Manager'da doğru konuma taşıyın
3. İzinleri 644 yapın

---

## ⚙️ ADIM 5: cPanel App Başlatma

### 5.1 App'i Başlatma
1. cPanel **.NET Core** paneline gidin
2. Oluşturduğunuz app'i seçin
3. **"Start"** butonuna tıklayın
4. **Status: Running** olduğunu kontrol edin

### 5.2 Domain/Subdomain Bağlama
```
# Ana domain için:
Document Root: public_html/

# Subdomain için:
Subdomain: app.yourdomain.com
Document Root: public_html/
```

---

## 🧪 ADIM 6: Test ve Doğrulama

### 6.1 Temel Test URL'leri
```
✅ Ana Sayfa: https://yourdomain.com/
✅ Health Check: https://yourdomain.com/health
✅ API Test: https://yourdomain.com/api/products
✅ Product List: https://yourdomain.com/Product
```

### 6.2 Health Check Kontrolü
Health check endpoint'i şu bilgileri verir:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connection successful"
    },
    {
      "name": "memory", 
      "status": "Healthy",
      "description": "Memory usage: XXX MB"
    }
  ]
}
```

### 6.3 Performans Testi
```bash
# API response testi
curl -X GET "https://yourdomain.com/api/products" -H "accept: application/json"

# Database test
curl -X GET "https://yourdomain.com/api/products/count"
```

---

## 🚨 ADIM 7: Troubleshooting

### 7.1 Yaygın Sorunlar ve Çözümler

#### ❌ "Application Failed to Start"
```bash
# Çözüm 1: Startup File kontrol
Startup File: ProductCatalogApp.dll (doğru path)

# Çözüm 2: Environment variables
ASPNETCORE_ENVIRONMENT=Production

# Çözüm 3: File permissions
chmod 755 data/
chmod 644 *.dll
```

#### ❌ "Database Connection Error"  
```bash
# Çözüm 1: Database path
SQLITE_DATABASE_PATH=~/data/products.db

# Çözüm 2: Data klasörü oluştur
mkdir data
chmod 755 data/

# Çözüm 3: SQLite file permissions
chmod 644 data/products.db
```

#### ❌ "404 Not Found"
```bash
# Çözüm 1: web.config kontrolü
web.config dosyası public_html'de olmalı

# Çözüm 2: URL Rewrite kontrol
cPanel URL Rewrite kuralları

# Çözüm 3: Document Root
Document Root: public_html/ (doğru ayar)
```

#### ❌ "Static Files Not Loading"
```bash
# Çözüm 1: wwwroot klasörü
wwwroot/ klasörünün yüklendiğini kontrol edin

# Çözüm 2: MIME types
cPanel'de .css, .js MIME types kontrol

# Çözüm 3: Cache issues
Browser cache temizleyin
```

### 7.2 Log Kontrolü
```bash
# cPanel Error Logs
cPanel > Error Logs > yourdomain.com

# Application Logs  
public_html/logs/app-{Date}.log

# IIS Logs (varsa)
cPanel > Raw Access Logs
```

### 7.3 Performance İyileştirme
```bash
# Memory Limit artır
cPanel > Select PHP Version > Options
memory_limit = 256M

# GZip Compression
cPanel > File Manager > .htaccess
# (web.config zaten compression içeriyor)

# CDN Entegrasyonu
Static dosyalar için Cloudflare vb.
```

---

## 🎉 ADIM 8: Production Optimizasyonları

### 8.1 SSL Certificate
```bash
# Let's Encrypt (Ücretsiz)
cPanel > SSL/TLS > Let's Encrypt

# Zorla HTTPS
cPanel > Redirects > www to non-www + HTTP to HTTPS
```

### 8.2 Backup Stratejisi
```bash
# Otomatik Backup
cPanel > Backup > Schedule Backups

# Manual Backup
- Database: data/products.db
- Files: public_html/
- Logs: logs/
```

### 8.3 Monitoring
```bash
# Health Check Monitoring
Uptime monitoring servisi kullanın:
- UptimeRobot
- Pingdom  
- StatusCake

# URL: https://yourdomain.com/health
```

### 8.4 Güvenlik
```bash
# File Permissions
find public_html/ -type f -exec chmod 644 {} \;
find public_html/ -type d -exec chmod 755 {} \;

# Sensitive Files
chmod 600 appsettings.Production.json
chmod 644 web.config

# .htaccess Protection
# (web.config zaten güvenlik headers içeriyor)
```

---

## 📞 Destek ve İletişim

### Deployment Sorunları
1. **Health check** URL'ini kontrol edin
2. **cPanel Error Logs** inceleyin  
3. **Environment variables** doğrulayın
4. **File permissions** kontrol edin

### Performans Sorunları
1. **Memory usage** health check ile izleyin
2. **Database size** kontrol edin
3. **Log files** boyutunu izleyin
4. **Cache** ayarlarını optimize edin

---

## ✅ Deployment Checklist

- [ ] **.NET 6.0** cPanel desteği var
- [ ] **Deployment script** çalıştırıldı
- [ ] **publish** klasörü hazırlandı
- [ ] **cPanel .NET Core app** oluşturuldu
- [ ] **Environment variables** ayarlandı
- [ ] **Dosyalar** public_html'e yüklendi
- [ ] **Database** klasörü oluşturuldu
- [ ] **Permissions** ayarlandı
- [ ] **App başlatıldı**
- [ ] **Test URL'leri** kontrol edildi
- [ ] **Health check** çalışıyor
- [ ] **SSL certificate** kuruldu
- [ ] **Backup** planlandı
- [ ] **Monitoring** ayarlandı

---

**🎯 Başarılı deployment sonrası uygulamanız tamamen optimized haliyle cPanel'de çalışacak!**

*Bu rehber, yapılan optimizasyonlar (cache pattern, sync/async uyumluluğu, cPanel uyumlu konfigürasyon) ile birlikte hazırlanmıştır.*