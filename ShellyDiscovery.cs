using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NINA.ShellyPower
{
    /// <summary>Une prise Shelly détectée sur le réseau local.</summary>
    public class ShellyDiscoveredDevice
    {
        public string Ip { get; set; }
        public string App { get; set; }
        public int Gen { get; set; } = 1;
        public string DeviceId { get; set; }
        public string Name { get; set; }

        public string Label
        {
            get
            {
                var model = string.IsNullOrWhiteSpace(App) ? (DeviceId ?? "?") : App;
                var custom = string.IsNullOrWhiteSpace(Name) ? "" : $" — « {Name} »";
                return $"{Ip} — {model} (Gen{Gen}){custom}";
            }
        }
    }

    /// <summary>
    /// Détection des prises Shelly sur le(s) réseau(x) local(aux).
    /// Méthode : balayage HTTP du /24 de chaque interface IPv4 active — chaque adresse
    /// est sondée sur http://&lt;ip&gt;/shelly avec un délai court (700 ms), en parallèle
    /// (64 threads). Une réponse JSON dont l'identifiant commence par « shelly » signe
    /// une prise Shelly (le /shelly est exposé par toutes les générations).
    /// Aucune dépendance externe (pas de mDNS/NuGet) : fonctionne hors ligne.
    /// </summary>
    public static class ShellyDiscovery
    {
        public static async Task<List<ShellyDiscoveredDevice>> DiscoverAsync(CancellationToken ct)
        {
            // Développe chaque base de sous-réseau (ex. 192.168.1) en adresses 1..254.
            var targets = GetLocalSubnets()
                .SelectMany(b => Enumerable.Range(1, 254).Select(i => $"{b}.{i}"))
                .Distinct()
                .ToList();
            var found = new List<ShellyDiscoveredDevice>();
            var lockObj = new object();

            using (var http = new HttpClient())
            {
                await Parallel.ForEachAsync(targets, new ParallelOptions
                {
                    // Parallélisme modéré : au-delà, Windows sature sa file de résolutions
                    // ARP (voisinage IP) lors du balayage de 254 adresses et même les
                    // prises existantes dépassent le délai d'attente (constaté en test).
                    MaxDegreeOfParallelism = 12,
                    CancellationToken = ct
                }, async (ip, token) =>
                {
                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                        cts.CancelAfter(900);
                        var json = await http.GetStringAsync($"http://{ip}/shelly", cts.Token);
                        var obj = JObject.Parse(json);
                        var id = (string)obj["id"] ?? "";
                        if (!id.StartsWith("shelly", StringComparison.OrdinalIgnoreCase))
                        {
                            return; // serveur HTTP quelconque, pas une prise Shelly
                        }

                        lock (lockObj)
                        {
                            found.Add(new ShellyDiscoveredDevice
                            {
                                Ip = ip,
                                App = (string)obj["app"],
                                Gen = (int?)obj["gen"] ?? 1,
                                DeviceId = id,
                                Name = (string)obj["name"]
                            });
                        }
                    }
                    catch
                    {
                        // Pas une prise Shelly (ou injoignable) : on passe à l'adresse suivante.
                    }
                });
            }

            return found
                .OrderBy(d => d.Ip, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Énumère les sous-réseaux /24 des interfaces IPv4 actives.</summary>
        private static IEnumerable<string> GetLocalSubnets()
        {
            var bases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    foreach (var ua in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            continue;
                        }

                        var parts = ua.Address.ToString().Split('.');
                        if (parts.Length != 4)
                        {
                            continue;
                        }

                        // Ignore les adresses de lien local (169.254.x.x).
                        if (parts[0] == "169" && parts[1] == "254")
                        {
                            continue;
                        }

                        bases.Add($"{parts[0]}.{parts[1]}.{parts[2]}");
                    }
                }
            }
            catch
            {
                // Au pire : aucun sous-réseau détecté, la liste sera vide.
            }

            return bases;
        }
    }
}