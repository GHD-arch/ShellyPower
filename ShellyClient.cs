using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Résultat d'un test / lecture d'état d'une prise : succès, message explicite,
    /// état ON/OFF et mesures de consommation (puissance, tension, courant, énergie).
    /// </summary>
    public class ShellyTestResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public bool? IsOn { get; set; }
        public string Device { get; set; }
        public double PowerW { get; set; }
        public double Voltage { get; set; }
        public double CurrentA { get; set; }
        public double EnergyKwh { get; set; }
    }

    /// <summary>
    /// Modèle JSON de l'API REST Gen1 :
    ///   GET http://&lt;ip&gt;/relay/0            -> {"ison": true|false, ...}
    ///   GET http://&lt;ip&gt;/relay/0?turn=on     -> état après allumage
    ///   GET http://&lt;ip&gt;/meter/0             -> {"power": 12.3, "total": 0.12 (kWh), ...}
    /// </summary>
    public class ShellyRelayStatus
    {
        [JsonProperty("ison")]
        public bool IsOn { get; set; }
    }

    /// <summary>
    /// Modèle JSON de l'API RPC Gen2/Gen3 (Shelly Plus / Pro / Gen3) :
    ///   GET http://&lt;ip&gt;/rpc/Switch.GetStatus?id=0
    ///     -> {"output": true|false, "apower": 12.3, "voltage": 230.1, "current": 0.05,
    ///         "aenergy": {"total": 0.12 (kWh), ...}, "temperature": {...}}
    ///   GET http://&lt;ip&gt;/rpc/Switch.Set?id=0&amp;on=true
    /// </summary>
    public class ShellyGen2Status
    {
        [JsonProperty("output")]
        public bool IsOn { get; set; }
    }

    /// <summary>
    /// Client HTTP minimal pour piloter et mesurer une prise Shelly.
    /// API Gen2/Gen3 RPC d'abord (état + mesures de consommation), repli automatique sur
    /// l'API Gen1 (/relay/0 + /meter/0).
    /// Un cache statique par IP (TTL 2 s) évite de doubler les requêtes quand NINA
    /// interroge à la fois le switch writable et le compteur de la même prise.
    /// </summary>
    public class ShellyClient
    {
        private const int CacheTtlSeconds = 2;

        private readonly HttpClient _http;

        private static readonly ConcurrentDictionary<string, (DateTime Time, ShellyTestResult Result)> _cache
            = new ConcurrentDictionary<string, (DateTime Time, ShellyTestResult Result)>(StringComparer.OrdinalIgnoreCase);

        public ShellyClient()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        /// <summary>URL du relais Gen1 (pour débogage).</summary>
        public static string RelayUrl(string ip) => $"http://{ip}/relay/0";

        /// <summary>
        /// Lit l'état complet d'une prise (état ON/OFF + consommation).
        /// Gen2/Gen3 RPC d'abord, repli Gen1. Cache 2 s sauf si force=true.
        /// </summary>
        public async Task<ShellyTestResult> GetStatusAsync(string ip, bool force = false, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return new ShellyTestResult { Ok = false, Message = "Adresse IP vide" };
            }

            var key = ip.Trim();
            if (!force && _cache.TryGetValue(key, out var hit) && (DateTime.UtcNow - hit.Time).TotalSeconds < CacheTtlSeconds)
            {
                return hit.Result;
            }

            var result = new ShellyTestResult();
            string device = null;
            try
            {
                device = await GetDeviceInfoAsync(ip, ct);
            }
            catch
            {
                // Pas bloquant : certains modèles ne répondent pas sur /shelly.
            }

            // ---- API RPC Gen2/Gen3 : output + apower + voltage + current + aenergy ----
            try
            {
                var json = await _http.GetStringAsync($"http://{ip}/rpc/Switch.GetStatus?id=0", ct);
                var obj = JObject.Parse(json);
                result.IsOn = (bool?)obj["output"];
                result.PowerW = (double?)obj["apower"] ?? 0;
                result.Voltage = (double?)obj["voltage"] ?? 0;
                result.CurrentA = (double?)obj["current"] ?? 0;
                result.EnergyKwh = (double?)obj["aenergy"]?["total"] ?? 0;
                result.Ok = true;
                result.Device = device;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // ---- Repli API Gen1 : /relay/0 + /meter/0 ----
                try
                {
                    var relay = JObject.Parse(await _http.GetStringAsync(RelayUrl(ip), ct));
                    result.IsOn = (bool?)relay["ison"];
                    try
                    {
                        var meter = JObject.Parse(await _http.GetStringAsync($"http://{ip}/meter/0", ct));
                        result.PowerW = (double?)meter["power"] ?? 0;
                        result.Voltage = (double?)meter["voltage"] ?? 0;
                        result.EnergyKwh = (double?)meter["total"] ?? 0;
                        if (result.Voltage > 0 && result.PowerW > 0)
                        {
                            result.CurrentA = result.PowerW / result.Voltage;
                        }
                    }
                    catch
                    {
                        // Certains Gen1 n'exposent pas /meter/0 : l'état reste valable.
                    }

                    result.Ok = true;
                    result.Device = device;
                }
                catch
                {
                    result.Ok = false;
                    result.Message = $"Injoignable ({ip})";
                }
            }

            if (result.Ok)
            {
                var state = result.IsOn == null ? "" : $" — prise {(result.IsOn.Value ? "allumée" : "éteinte")}";
                var power = result.PowerW > 0 ? $" — {result.PowerW:0.#} W" : "";
                result.Message = $"OK{(device != null ? $" — {device}" : "")}{state}{power}";
            }

            _cache[key] = (DateTime.UtcNow, result);
            return result;
        }

        /// <summary>Identifie le modèle : GET /shelly -> {"app":"PlusPlugS","gen":2,...}</summary>
        private async Task<string> GetDeviceInfoAsync(string ip, CancellationToken ct)
        {
            var info = await _http.GetStringAsync($"http://{ip}/shelly", ct);
            var obj = JObject.Parse(info);
            var app = (string)obj["app"];
            var gen = (int?)obj["gen"];
            return app != null ? $"{app} (Gen{gen ?? 1})" : null;
        }

        /// <summary>Lit l'état allumé/éteint de la prise.</summary>
        public async Task<bool> GetIsOnAsync(string ip, CancellationToken ct = default)
        {
            var status = await GetStatusAsync(ip, false, ct);
            return status.IsOn ?? false;
        }

        /// <summary>Allume la prise et renvoie son état rafraîchi.</summary>
        public async Task<bool> TurnOnAsync(string ip, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            try
            {
                await _http.GetStringAsync($"{RelayUrl(ip)}?turn=on", ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                try
                {
                    await _http.GetStringAsync($"http://{ip}/rpc/Switch.Set?id=0&on=true", ct);
                }
                catch
                {
                    // La relecture d'état ci-dessous fera foi.
                }
            }

            var status = await GetStatusAsync(ip, true, ct);
            return status.IsOn ?? false;
        }

        /// <summary>Éteint la prise et renvoie son état rafraîchi.</summary>
        public async Task<bool> TurnOffAsync(string ip, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            try
            {
                await _http.GetStringAsync($"{RelayUrl(ip)}?turn=off", ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                try
                {
                    await _http.GetStringAsync($"http://{ip}/rpc/Switch.Set?id=0&on=false", ct);
                }
                catch
                {
                    // La relecture d'état ci-dessous fera foi.
                }
            }

            var status = await GetStatusAsync(ip, true, ct);
            return status.IsOn ?? false;
        }

        /// <summary>
        /// Teste la prise : identifie le modèle puis lit l'état complet (avec consommation).
        /// Renvoie un résultat explicite (modèle détecté, ou cause de l'échec).
        /// </summary>
        public async Task<ShellyTestResult> TestAsync(string ip, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return new ShellyTestResult { Ok = false, Message = "Adresse IP vide" };
            }

            var result = await GetStatusAsync(ip, true, ct);
            if (result.Ok)
            {
                result.Message = "OK" + (result.Device != null ? $" — {result.Device}" : "")
                    + (result.IsOn == null ? "" : $" — prise {(result.IsOn.Value ? "allumée" : "éteinte")}")
                    + (result.PowerW > 0 ? $" — {result.PowerW:0.#} W" : "");
            }

            return result;
        }
    }
}