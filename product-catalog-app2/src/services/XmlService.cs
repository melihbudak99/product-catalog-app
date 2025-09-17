using System.Xml.Serialization;
using System.Text;
using System.Security;
using product_catalog_app.src.models;

namespace product_catalog_app.src.services
{
    public class XmlService
    {
        public string ExportToXml(List<ProductXml> products)
        {
            var catalog = new ProductCatalog { Products = products };

            var serializer = new XmlSerializer(typeof(ProductCatalog));
            using var stringWriter = new Utf8StringWriter(); // UTF-8 StringWriter kullan
            using var xmlWriter = System.Xml.XmlWriter.Create(stringWriter, new System.Xml.XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8, // UTF-8 encoding
                OmitXmlDeclaration = false // XML declaration dahil et
            });

            serializer.Serialize(xmlWriter, catalog);
            return stringWriter.ToString();
        }

        public List<ProductXml> ImportFromXml(string xmlContent)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ProductCatalog));
                using var stringReader = new StringReader(xmlContent);

                var catalog = (ProductCatalog?)serializer.Deserialize(stringReader);
                return catalog?.Products ?? new List<ProductXml>();
            }
            catch (Exception ex)
            {
                throw new Exception($"XML içe aktarma hatası: {ex.Message}", ex);
            }
        }

        public void SaveToFile(string xmlContent, string filePath)
        {
            File.WriteAllText(filePath, xmlContent, Encoding.UTF8);
        }

        public string LoadFromFile(string filePath)
        {
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        public async Task<Stream> ExportToXmlStream(List<ProductXml> products)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, new UTF8Encoding(false));

            await writer.WriteLineAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            await writer.WriteLineAsync("<ProductCatalog>");

            foreach (var product in products)
            {
                await writer.WriteLineAsync("  <Product>");
                await writer.WriteLineAsync($"    <Id>{product.Id}</Id>");
                await writer.WriteLineAsync($"    <Name>{SecurityElement.Escape(product.Name)}</Name>");
                // ... diğer alanlar
                await writer.WriteLineAsync("  </Product>");
            }

            await writer.WriteLineAsync("</ProductCatalog>");
            await writer.FlushAsync();

            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// HTML etiketlerini temizler, sadece düz metin döndürür
        /// </summary>
        public string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Basic regex to remove HTML tags
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }
    }

    // UTF-8 StringWriter sınıfı
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}