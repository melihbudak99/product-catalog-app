CREATE TABLE "Categories" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "UpdatedDate" TEXT NOT NULL
);


CREATE TABLE "XmlProducts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_XmlProducts" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "DescriptionHtml" TEXT NOT NULL,
    "DescriptionPlain" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "Brand" TEXT NOT NULL,
    "SKU" TEXT NOT NULL,
    "Weight" TEXT NOT NULL,
    "Desi" TEXT NOT NULL,
    "Width" TEXT NOT NULL,
    "Height" TEXT NOT NULL,
    "Depth" TEXT NOT NULL,
    "WarrantyMonths" INTEGER NOT NULL,
    "Material" TEXT NOT NULL,
    "Color" TEXT NOT NULL,
    "EanCode" TEXT NOT NULL,
    "Features" TEXT NOT NULL,
    "Notes" TEXT NOT NULL,
    "TrendyolBarcode" TEXT NOT NULL,
    "HepsiburadaBarcode" TEXT NOT NULL,
    "HepsiburadaSellerStockCode" TEXT NOT NULL,
    "KoctasBarcode" TEXT NOT NULL,
    "KoctasIstanbulBarcode" TEXT NOT NULL,
    "HepsiburadaTedarikBarcode" TEXT NOT NULL,
    "PttAvmBarcode" TEXT NOT NULL,
    "PazaramaBarcode" TEXT NOT NULL,
    "HaceyapiBarcode" TEXT NOT NULL,
    "AmazonBarcode" TEXT NOT NULL,
    "N11CatalogId" TEXT NOT NULL,
    "N11ProductCode" TEXT NOT NULL,
    "SpareBarcode1" TEXT NOT NULL,
    "SpareBarcode2" TEXT NOT NULL,
    "SpareBarcode3" TEXT NOT NULL,
    "SpareBarcode4" TEXT NOT NULL,
    "LogoBarcodes" TEXT NOT NULL,
    "KoctasEanBarcode" TEXT NOT NULL,
    "KoctasEanIstanbulBarcode" TEXT NOT NULL,
    "PttUrunStokKodu" TEXT NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "UpdatedDate" TEXT NULL,
    "ImageUrl1" TEXT NOT NULL,
    "ImageUrl2" TEXT NOT NULL,
    "ImageUrl3" TEXT NOT NULL,
    "ImageUrl4" TEXT NOT NULL,
    "ImageUrl5" TEXT NOT NULL,
    "ImageUrl6" TEXT NOT NULL,
    "ImageUrl7" TEXT NOT NULL,
    "ImageUrl8" TEXT NOT NULL,
    "ImageUrl9" TEXT NOT NULL,
    "ImageUrl10" TEXT NOT NULL,
    "MarketplaceImageUrl1" TEXT NOT NULL,
    "MarketplaceImageUrl2" TEXT NOT NULL,
    "MarketplaceImageUrl3" TEXT NOT NULL,
    "MarketplaceImageUrl4" TEXT NOT NULL,
    "MarketplaceImageUrl5" TEXT NOT NULL,
    "MarketplaceImageUrl6" TEXT NOT NULL,
    "MarketplaceImageUrl7" TEXT NOT NULL,
    "MarketplaceImageUrl8" TEXT NOT NULL,
    "MarketplaceImageUrl9" TEXT NOT NULL,
    "MarketplaceImageUrl10" TEXT NOT NULL,
    "VideoUrl1" TEXT NOT NULL,
    "VideoUrl2" TEXT NOT NULL,
    "VideoUrl3" TEXT NOT NULL,
    "VideoUrl4" TEXT NOT NULL,
    "VideoUrl5" TEXT NOT NULL,
    "IsArchived" INTEGER NOT NULL
);


CREATE TABLE "Products" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Products" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "SKU" TEXT NULL,
    "Brand" TEXT NULL,
    "Category" TEXT NULL,
    "Description" TEXT NULL,
    "Features" TEXT NULL,
    "ImageUrl" TEXT NULL,
    "Weight" decimal(18,2) NOT NULL,
    "Desi" TEXT NOT NULL,
    "Width" TEXT NOT NULL,
    "Height" TEXT NOT NULL,
    "Depth" TEXT NOT NULL,
    "Length" decimal(18,2) NULL,
    "WarrantyMonths" INTEGER NOT NULL,
    "Material" TEXT NULL,
    "Color" TEXT NULL,
    "EanCode" TEXT NULL,
    "Notes" TEXT NULL,
    "TrendyolBarcode" TEXT NULL,
    "HepsiburadaBarcode" TEXT NULL,
    "HepsiburadaSellerStockCode" TEXT NULL,
    "KoctasBarcode" TEXT NULL,
    "KoctasIstanbulBarcode" TEXT NULL,
    "HepsiburadaTedarikBarcode" TEXT NULL,
    "PttAvmBarcode" TEXT NULL,
    "PazaramaBarcode" TEXT NULL,
    "HaceyapiBarcode" TEXT NULL,
    "AmazonBarcode" TEXT NULL,
    "N11CatalogId" TEXT NULL,
    "N11ProductCode" TEXT NULL,
    "SpareBarcode1" TEXT NULL,
    "SpareBarcode2" TEXT NULL,
    "SpareBarcode3" TEXT NULL,
    "SpareBarcode4" TEXT NULL,
    "LogoBarcodes" TEXT NULL,
    "KoctasEanBarcode" TEXT NOT NULL,
    "KoctasEanIstanbulBarcode" TEXT NOT NULL,
    "PttUrunStokKodu" TEXT NOT NULL,
    "EntegraUrunId" TEXT NULL,
    "EntegraUrunKodu" TEXT NULL,
    "EntegraBarkod" TEXT NULL,
    "KlozetKanalYapisi" TEXT NULL,
    "KlozetTipi" TEXT NULL,
    "KlozetKapakCinsi" TEXT NULL,
    "KlozetMontajTipi" TEXT NULL,
    "LawaboSuTasmaDeligi" TEXT NULL,
    "LawaboArmaturDeligi" TEXT NULL,
    "LawaboTipi" TEXT NULL,
    "LawaboOzelligi" TEXT NULL,
    "BataryaCikisUcuUzunlugu" TEXT NULL,
    "BataryaYuksekligi" TEXT NULL,
    "KabinTipi" TEXT NOT NULL,
    "ImageUrl1" TEXT NOT NULL,
    "ImageUrl2" TEXT NOT NULL,
    "ImageUrl3" TEXT NOT NULL,
    "ImageUrl4" TEXT NOT NULL,
    "ImageUrl5" TEXT NOT NULL,
    "ImageUrl6" TEXT NOT NULL,
    "ImageUrl7" TEXT NOT NULL,
    "ImageUrl8" TEXT NOT NULL,
    "ImageUrl9" TEXT NOT NULL,
    "ImageUrl10" TEXT NOT NULL,
    "ImageUrls" TEXT NOT NULL,
    "MarketplaceImageUrls" TEXT NOT NULL,
    "VideoUrls" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    "UpdatedDate" TEXT NULL,
    "IsArchived" INTEGER NOT NULL,
    "CategoryId" INTEGER NULL,
    CONSTRAINT "FK_Products_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL
);


CREATE INDEX "IX_Categories_IsActive" ON "Categories" ("IsActive");


CREATE UNIQUE INDEX "IX_Categories_Name" ON "Categories" ("Name");


CREATE INDEX "IX_Products_Brand" ON "Products" ("Brand");


CREATE INDEX "IX_Products_Category" ON "Products" ("Category");


CREATE INDEX "IX_Products_Category_Brand" ON "Products" ("Category", "Brand");


CREATE INDEX "IX_Products_CategoryId" ON "Products" ("CategoryId");


CREATE INDEX "IX_Products_CreatedDate" ON "Products" ("CreatedDate");


CREATE INDEX "IX_Products_IsArchived" ON "Products" ("IsArchived");


CREATE INDEX "IX_Products_IsArchived_Category_Brand" ON "Products" ("IsArchived", "Category", "Brand");


CREATE INDEX "IX_Products_IsArchived_Name" ON "Products" ("IsArchived", "Name");


CREATE INDEX "IX_Products_Name" ON "Products" ("Name");


CREATE INDEX "IX_Products_SKU" ON "Products" ("SKU");


