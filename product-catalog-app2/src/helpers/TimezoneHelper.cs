using System;

namespace ProductCatalogApp.Helpers
{
    public static class TimezoneHelper
    {
        // Türkiye saat dilimi (UTC+3)
        private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Turkey Standard Time", 
            TimeSpan.FromHours(3), 
            "Turkey Standard Time", 
            "Turkey Standard Time"
        );

        /// <summary>
        /// UTC tarihini Türkiye saat dilimine çevirir
        /// </summary>
        /// <param name="utcDateTime">UTC tarih</param>
        /// <returns>Türkiye saatine çevrilmiş tarih</returns>
        public static DateTime ToTurkeyTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkeyTimeZone);
            }
            
            // Eğer kind belirtilmemişse UTC kabul ediyoruz
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), TurkeyTimeZone);
        }

        /// <summary>
        /// Nullable UTC tarihini Türkiye saat dilimine çevirir
        /// </summary>
        /// <param name="utcDateTime">UTC tarih (nullable)</param>
        /// <returns>Türkiye saatine çevrilmiş tarih (nullable)</returns>
        public static DateTime? ToTurkeyTime(this DateTime? utcDateTime)
        {
            return utcDateTime?.ToTurkeyTime();
        }

        /// <summary>
        /// UTC tarihini Türkiye saat diliminde formatlar
        /// </summary>
        /// <param name="utcDateTime">UTC tarih</param>
        /// <param name="format">Tarih formatı (varsayılan: "dd.MM.yyyy HH:mm")</param>
        /// <returns>Formatlanmış Türkiye saati</returns>
        public static string ToTurkeyTimeString(this DateTime utcDateTime, string format = "dd.MM.yyyy HH:mm")
        {
            return utcDateTime.ToTurkeyTime().ToString(format);
        }

        /// <summary>
        /// Nullable UTC tarihini Türkiye saat diliminde formatlar
        /// </summary>
        /// <param name="utcDateTime">UTC tarih (nullable)</param>
        /// <param name="format">Tarih formatı (varsayılan: "dd.MM.yyyy HH:mm")</param>
        /// <param name="nullText">Null değer için gösterilecek metin (varsayılan: "-")</param>
        /// <returns>Formatlanmış Türkiye saati veya null text</returns>
        public static string ToTurkeyTimeString(this DateTime? utcDateTime, string format = "dd.MM.yyyy HH:mm", string nullText = "-")
        {
            return utcDateTime?.ToTurkeyTimeString(format) ?? nullText;
        }

        /// <summary>
        /// Türkiye saatini UTC'ye çevirir (form gönderimlerinde kullanım için)
        /// </summary>
        /// <param name="turkeyDateTime">Türkiye saati</param>
        /// <returns>UTC tarih</returns>
        public static DateTime ToUtcFromTurkeyTime(this DateTime turkeyDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(turkeyDateTime, TurkeyTimeZone);
        }

        /// <summary>
        /// Şu anki Türkiye saatini verir
        /// </summary>
        /// <returns>Şu anki Türkiye saati</returns>
        public static DateTime NowInTurkey()
        {
            return DateTime.UtcNow.ToTurkeyTime();
        }
    }
}