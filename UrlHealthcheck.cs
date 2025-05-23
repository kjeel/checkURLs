using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UrlHealthCheckFunction.Models;

namespace UrlHealthCheckFunction
{
    public static class UrlHealthCheck
    {
        // Wiederverwendbarer HttpClient mit 10s Timeout
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        [FunctionName("UrlHealthCheck")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            // projectRoot: zwei Ebenen über bin/output = Projektverzeichnis
            var projectRoot = Path.GetFullPath(
                Path.Combine(context.FunctionAppDirectory, "..", ".."));
            var logFilePath = Path.Combine(projectRoot, "logs.txt");
            log.LogInformation("Writing logs to: {LogFile}", logFilePath);

            // 1) Request-Body einlesen + JSON → InputModel
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            InputModel input;
            try
            {
                input = JsonConvert.DeserializeObject<InputModel>(requestBody);
                if (input?.Urls == null || !input.Urls.Any())
                    return new BadRequestObjectResult(
                        "JSON muss ein nicht-leeres 'Urls'-Array enthalten.");
            }
            catch (JsonException je)
            {
                log.LogError(je, "Invalid JSON payload");
                return new BadRequestObjectResult("Ungültiges JSON.");
            }

            // 2) Start-Eintrag ins Log
            await AppendLogAsync(
                $"[{DateTime.UtcNow:O}] START – URLs: {string.Join(", ", input.Urls)}{Environment.NewLine}",
                logFilePath, log);

            var results = new List<ResultModel>();

            // 3) Für jede URL HTTP-Check + SSL-Check
            foreach (var url in input.Urls)
            {
                var result = await CheckUrlAsync(url);
                results.Add(result);

                // Log-Zeile erweitern um SSL-Infos
                var line = $"[{DateTime.UtcNow:O}] {url} → " +
                           $"Status={result.Status}, " +
                           $"Reachable={result.Reachable}, " +
                           $"ResponseTime={result.ResponseTime}ms, " +
                           $"CertPresent={result.CertificatePresent}, " +
                           $"CertExpiry={result.CertificateExpiry:O}{Environment.NewLine}";
                await AppendLogAsync(line, logFilePath, log);

                log.LogInformation(
                    "Checked {Url}: Status={Status}, Reachable={Reachable}, Time={Time}ms, " +
                    "CertPresent={CertPresent}, CertExpiry={CertExpiry}",
                    result.Url, result.Status, result.Reachable,
                    result.ResponseTime, result.CertificatePresent, result.CertificateExpiry);
            }

            // 4) Trenner
            await AppendLogAsync(Environment.NewLine, logFilePath, log);

            // 5) Ergebnisse als JSON zurückliefern
            return new OkObjectResult(results);
        }

        /// <summary>
        /// Hängt Text an logs.txt an, fängt Fehler intern ab.
        /// </summary>
        private static async Task AppendLogAsync(
            string text, string path, ILogger log)
        {
            try
            {
                await File.AppendAllTextAsync(path, text);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Fehler beim Schreiben in Log-Datei {Path}", path);
            }
        }

        /// <summary>
        /// Führt HTTP-GET aus und misst Zeit. Liest danach das SSL-Zertifikat aus.
        /// </summary>
        private static async Task<ResultModel> CheckUrlAsync(string url)
        {
            var sw = Stopwatch.StartNew();
            var result = new ResultModel
            {
                Url = url,
                Timestamp = DateTime.UtcNow
            };

            // HTTP-Check
            try
            {
                var response = await _httpClient.GetAsync(EnsureHttpScheme(url));
                sw.Stop();
                result.Status = (int)response.StatusCode;
                result.Reachable = response.IsSuccessStatusCode;
            }
            catch
            {
                sw.Stop();
                result.Status = -1;
                result.Reachable = false;
            }
            result.ResponseTime = sw.ElapsedMilliseconds;

            // SSL-Zertifikat prüfen
            var hostOnly = new Uri(EnsureHttpScheme(url)).Host;
            var expiry = GetCertificateExpiry(hostOnly);
            result.CertificatePresent = expiry.HasValue;
            result.CertificateExpiry = expiry;

            return result;
        }

        /// <summary>
        /// Öffnet per TcpClient+SslStream eine TLS-Verbindung
        /// und liest das Zertifikat-Ablaufdatum aus.
        /// </summary>
        private static DateTime? GetCertificateExpiry(
            string host, int port = 443)
        {
            try
            {
                using var tcp = new TcpClient(host, port);
                using var ssl = new SslStream(tcp.GetStream(), false);
                ssl.AuthenticateAsClient(host);
                var cert = new X509Certificate2(ssl.RemoteCertificate);
                return cert.NotAfter.ToUniversalTime();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Stellt sicher, dass die URL mit http(s):// beginnt.
        /// </summary>
        private static string EnsureHttpScheme(string url)
            => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? url
                : "https://" + url;
    }
}
