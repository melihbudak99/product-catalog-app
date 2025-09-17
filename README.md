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

### cPanele yukleme adimlari:
1. publish klasorunun tum icerigini cPanel File Managerda public_html klasorune yukleyin
2. cPanelde .NET Core ayarlarina gidin
3. Startup File olarak ProductCatalogApp.dll belirtin
4. Environment olarak Production secin
