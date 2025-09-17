-- Add Entegra columns to Products table
ALTER TABLE Products ADD COLUMN EntegraUrunId TEXT;
ALTER TABLE Products ADD COLUMN EntegraUrunKodu TEXT;
ALTER TABLE Products ADD COLUMN EntegraBarkod TEXT;

-- Also add missing columns to XmlProducts table
ALTER TABLE XmlProducts ADD COLUMN HepsiburadaSellerStockCode TEXT;
ALTER TABLE XmlProducts ADD COLUMN N11CatalogId TEXT;
ALTER TABLE XmlProducts ADD COLUMN N11ProductCode TEXT;