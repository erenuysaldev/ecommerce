namespace ECommerceProject.Core.Configuration
{
    public class JwtConfig
    {
        public string Key { get; set; }          // Token imzalama anahtarı
        public string Issuer { get; set; }       // Token'ı oluşturan
        public string Audience { get; set; }     // Token'ı kullanacak olan
        public int DurationInMinutes { get; set; } // Token süresi
    }
} 