using System;
using System.Collections.Generic;
using System.Linq;
using NINA.Profile.Interfaces;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Une prise configurée : un nom de fonction (ex. "Alimentation", "Caméra"), une adresse
    /// IP et une protection « confirmation avant extinction ».
    /// </summary>
    public class ShellyPlugConfig
    {
        public string Name { get; set; } = "";
        public string Ip { get; set; } = "";

        /// <summary>Si vrai, une confirmation est demandée avant d'éteindre la prise (actions manuelles).</summary>
        public bool ProtectOff { get; set; } = true;
    }

    /// <summary>
    /// Accès persistant aux options du plugin via <see cref="IPluginOptionsAccessor"/> (NINA).
    /// Permet de stocker jusqu'à 4 prises nommées + leur adresse IP, et de les recharger.
    /// </summary>
    public class ShellyOptions
    {
        /// <summary>Nombre de prises gérées par le plugin.</summary>
        public const int PlugCount = 4;

        private const string Prefix = "ShellyPower";
        private readonly IPluginOptionsAccessor _options;

        public ShellyOptions(IPluginOptionsAccessor options)
        {
            _options = options;
        }

        public string GetPlugName(int index) => _options.GetValueString($"{Prefix}_Plug{index}_Name", $"Prise {index + 1}");
        public void SetPlugName(int index, string value) => _options.SetValueString($"{Prefix}_Plug{index}_Name", value ?? "");

        public string GetPlugIp(int index) => _options.GetValueString($"{Prefix}_Plug{index}_Ip", "");
        public void SetPlugIp(int index, string value) => _options.SetValueString($"{Prefix}_Plug{index}_Ip", value ?? "");

        /// <summary>Protection : demander confirmation avant d'éteindre (défaut : activé).</summary>
        public bool GetPlugProtectOff(int index) => _options.GetValueBoolean($"{Prefix}_Plug{index}_ProtectOff", true);
        public void SetPlugProtectOff(int index, bool value) => _options.SetValueBoolean($"{Prefix}_Plug{index}_ProtectOff", value);

        public IReadOnlyList<ShellyPlugConfig> GetPlugs()
        {
            var list = new List<ShellyPlugConfig>();
            for (var i = 0; i < PlugCount; i++)
            {
                list.Add(new ShellyPlugConfig { Name = GetPlugName(i), Ip = GetPlugIp(i), ProtectOff = GetPlugProtectOff(i) });
            }

            return list;
        }

        /// <summary>Renvoie les noms des prises ayant une IP renseignée.</summary>
        public IReadOnlyList<string> GetAvailablePlugNames()
        {
            return GetPlugs()
                .Where(p => !string.IsNullOrWhiteSpace(p.Ip))
                .Select(p => string.IsNullOrWhiteSpace(p.Name) ? p.Ip : $"{p.Name} ({p.Ip})")
                .ToList();
        }
    }
}
