using System;

namespace UrlHealthCheckFunction.Models
{
    public class ResultModel
{
    public string Url { get; set; }
    public int? Status { get; set; }
    public bool Reachable { get; set; }
    public DateTime Timestamp { get; set; }
    public long ResponseTime { get; set; }

    // Neu:
    /// <summary>Gibt an, ob ein gültiges SSL-Zertifikat vorliegt.</summary>
    public bool CertificatePresent { get; set; }

    /// <summary>Ablaufdatum des Zertifikats (UTC). Null, wenn keins vorhanden.</summary>
    public DateTime? CertificateExpiry { get; set; }
}

}
