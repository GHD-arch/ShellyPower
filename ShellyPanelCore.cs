using NINA.Profile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NINA.Profile.Interfaces;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Logique partagée du panneau Shelly Power : état des 4 prises (nom, IP, résultat,
    /// état ON/OFF) + commandes (Test, Save, On, Off, Refresh).
    /// Sert de DataContext effectif (propriété « Core ») pour TOUTES les surfaces où la vue
    /// est affichée :
    ///   - le panneau dockable de la fenêtre principale (ShellyOptionsVM.Core),
    ///   - la page Plugins des Options de NINA (ShellyPowerPlugin.Core) — le DataContext y
    ///     est le manifeste, d'où l'indirection,
    ///   - la fenêtre de l'engrenage « Setup » (ShellyOptionsVM.Core).
    /// Chaque saisie est enregistrée immédiatement dans le profil NINA (PluginOptionsAccessor).
    /// </summary>
    public class ShellyPanelCore : INotifyPropertyChanged
    {
        private readonly ShellyOptions _options;
        private readonly string[] _names = new string[ShellyOptions.PlugCount];
        private readonly string[] _ips = new string[ShellyOptions.PlugCount];
        private readonly string[] _results = new string[ShellyOptions.PlugCount];
        private readonly bool?[] _states = new bool?[ShellyOptions.PlugCount];
        private readonly double?[] _power = new double?[ShellyOptions.PlugCount];
        private string _statusMessage = "";

        public ShellyPanelCore(IProfileService profileService)
        {
            _options = new ShellyOptions(new PluginOptionsAccessor(profileService, PluginGuid));
            Load();

            TestCommand = new RelayCommandAsync(TestAsync);
            SaveCommand = new RelayCommand(_ => SaveAll());
            OnCommand = new RelayCommandAsync(async i => await PowerAsync(i, true));
            OffCommand = new RelayCommandAsync(async i => await PowerAsync(i, false));
            RefreshCommand = new RelayCommandAsync(_ => RefreshStatesAsync());
            DetectCommand = new RelayCommand(_ => ShowDetectionWindow());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand TestCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand OnCommand { get; }
        public ICommand OffCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand DetectCommand { get; }

        // ----- Propriétés par prise (sauvegarde automatique à la saisie) -----
        public string Plug0Name { get => _names[0]; set { if (SetPlug(0, value, true)) OnPropertyChanged(nameof(Plug0Name)); } }
        public string Plug0Ip { get => _ips[0]; set { if (SetPlug(0, value, false)) OnPropertyChanged(nameof(Plug0Ip)); } }
        public string Plug0Result { get => _results[0]; }
        public string Plug0State => StateText(0);
        public string Plug1Name { get => _names[1]; set { if (SetPlug(1, value, true)) OnPropertyChanged(nameof(Plug1Name)); } }
        public string Plug1Ip { get => _ips[1]; set { if (SetPlug(1, value, false)) OnPropertyChanged(nameof(Plug1Ip)); } }
        public string Plug1Result { get => _results[1]; }
        public string Plug1State => StateText(1);
        public string Plug2Name { get => _names[2]; set { if (SetPlug(2, value, true)) OnPropertyChanged(nameof(Plug2Name)); } }
        public string Plug2Ip { get => _ips[2]; set { if (SetPlug(2, value, false)) OnPropertyChanged(nameof(Plug2Ip)); } }
        public string Plug2Result { get => _results[2]; }
        public string Plug2State => StateText(2);
        public string Plug3Name { get => _names[3]; set { if (SetPlug(3, value, true)) OnPropertyChanged(nameof(Plug3Name)); } }
        public string Plug3Ip { get => _ips[3]; set { if (SetPlug(3, value, false)) OnPropertyChanged(nameof(Plug3Ip)); } }
        public string Plug3Result { get => _results[3]; }
        public string Plug3State => StateText(3);

        // ----- Protection « confirmation avant extinction » par prise -----
        private readonly bool[] _protectOff = new bool[ShellyOptions.PlugCount] { true, true, true, true };

        public bool Plug0ProtectOff { get => _protectOff[0]; set { if (SetProtect(0, value)) OnPropertyChanged(nameof(Plug0ProtectOff)); } }
        public bool Plug1ProtectOff { get => _protectOff[1]; set { if (SetProtect(1, value)) OnPropertyChanged(nameof(Plug1ProtectOff)); } }
        public bool Plug2ProtectOff { get => _protectOff[2]; set { if (SetProtect(2, value)) OnPropertyChanged(nameof(Plug2ProtectOff)); } }
        public bool Plug3ProtectOff { get => _protectOff[3]; set { if (SetProtect(3, value)) OnPropertyChanged(nameof(Plug3ProtectOff)); } }

        private bool SetProtect(int index, bool value)
        {
            if (_protectOff[index] == value)
            {
                return false;
            }

            _protectOff[index] = value;
            _options.SetPlugProtectOff(index, value);
            StatusMessage = ShellyStrings.L("Sauvegardé automatiquement", "Auto-saved");
            return true;
        }

        /// <summary>Message de confirmation (dernier enregistrement).</summary>
        public string StatusMessage { get => _statusMessage; private set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool SetPlug(int index, string value, bool isName)
        {
            value = value ?? "";
            var array = isName ? _names : _ips;
            if (array[index] == value)
            {
                return false;
            }

            array[index] = value;
            if (isName)
            {
                _options.SetPlugName(index, value);
            }
            else
            {
                _options.SetPlugIp(index, value);
            }

            StatusMessage = ShellyStrings.L("Sauvegardé automatiquement", "Auto-saved");
            return true;
        }

        private void Load()
        {
            for (var i = 0; i < ShellyOptions.PlugCount; i++)
            {
                _names[i] = _options.GetPlugName(i);
                _ips[i] = _options.GetPlugIp(i);
                _protectOff[i] = _options.GetPlugProtectOff(i);
            }

            foreach (var p in new[] { nameof(Plug0Name), nameof(Plug0Ip), nameof(Plug1Name), nameof(Plug1Ip), nameof(Plug2Name), nameof(Plug2Ip), nameof(Plug3Name), nameof(Plug3Ip), nameof(Plug0State), nameof(Plug1State), nameof(Plug2State), nameof(Plug3State), nameof(Plug0ProtectOff), nameof(Plug1ProtectOff), nameof(Plug2ProtectOff), nameof(Plug3ProtectOff) })
            {
                OnPropertyChanged(p);
            }
        }

        private void SaveAll()
        {
            for (var i = 0; i < ShellyOptions.PlugCount; i++)
            {
                _options.SetPlugName(i, _names[i] ?? "");
                _options.SetPlugIp(i, _ips[i] ?? "");
            }

            StatusMessage = ShellyStrings.L("Enregistré ✓ (", "Saved ✓ (") + DateTime.Now.ToString("HH:mm:ss") + ")";
        }

        private async System.Threading.Tasks.Task TestAsync(int index)
        {
            var ip = _ips[index];
            if (string.IsNullOrWhiteSpace(ip))
            {
                SetResult(index, ShellyStrings.L("✖ Saisissez d'abord l'adresse IP de cette prise", "✖ Enter the IP address of this plug first"));
                return;
            }

            SetResult(index, ShellyStrings.L("… test en cours", "… testing…"));
            var result = await new ShellyClient().TestAsync(ip);
            _power[index] = result.Ok ? result.PowerW : null;
            SetResult(index, (result.Ok ? "✔ " : "✖ ") + result.Message);
            OnPropertyChanged($"Plug{index}State");
        }

        private void SetResult(int index, string text)
        {
            _results[index] = text;
            OnPropertyChanged($"Plug{index}Result");
        }

        private string StateText(int index)
        {
            if (string.IsNullOrWhiteSpace(_ips[index]))
            {
                return "—";
            }

            var state = _states[index] == null
                ? ShellyStrings.L("inconnu", "unknown")
                : (_states[index].Value
                    ? ShellyStrings.L("● allumée", "● on")
                    : ShellyStrings.L("○ éteinte", "○ off"));

            if (_power[index] != null && _power[index] > 0)
            {
                state += $" · {_power[index]:0.#} W";
            }

            return state;
        }

        private async System.Threading.Tasks.Task PowerAsync(int index, bool on)
        {
            var ip = _ips[index];
            if (string.IsNullOrWhiteSpace(ip))
            {
                SetResult(index, ShellyStrings.L("✖ Configurez d'abord l'adresse IP de cette prise", "✖ Configure the IP address of this plug first"));
                return;
            }

            if (!on && _protectOff[index])
            {
                var plugName = string.IsNullOrWhiteSpace(_names[index]) ? $"Prise {index + 1}" : _names[index];
                var answer = System.Windows.MessageBox.Show(
                    ShellyStrings.L($"Éteindre la prise « {plugName} » ({ip}) ?", $"Turn off plug « {plugName} » ({ip}) ?"),
                    ShellyStrings.L("Shelly Power — Confirmation", "Shelly Power — Confirmation"),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxResult.No);
                if (answer != System.Windows.MessageBoxResult.Yes)
                {
                    SetResult(index, ShellyStrings.L("✖ Extinction annulée (protection)", "✖ Turn-off cancelled (protected)"));
                    return;
                }
            }

            SetResult(index, on
                ? ShellyStrings.L("… allumage en cours", "… turning on…")
                : ShellyStrings.L("… extinction en cours", "… turning off…"));
            try
            {
                var client = new ShellyClient();
                if (on)
                {
                    await client.TurnOnAsync(ip);
                }
                else
                {
                    await client.TurnOffAsync(ip);
                }

                var status = await client.GetStatusAsync(ip, true);
                _states[index] = status.IsOn;
                _power[index] = status.Ok ? status.PowerW : null;
                SetResult(index, status.Ok ? "✔ " + status.Message : "✖ " + ShellyStrings.L("Injoignable", "Unreachable") + " (" + ip + ")");
            }
            catch
            {
                _states[index] = null;
                _power[index] = null;
                SetResult(index, "✖ " + ShellyStrings.L("Injoignable", "Unreachable") + " (" + ip + ")");
            }

            OnPropertyChanged($"Plug{index}State");
        }

        private async System.Threading.Tasks.Task RefreshStatesAsync()
        {
            var client = new ShellyClient();
            var tasks = new List<System.Threading.Tasks.Task>();
            for (var i = 0; i < ShellyOptions.PlugCount; i++)
            {
                if (string.IsNullOrWhiteSpace(_ips[i]))
                {
                    continue;
                }

                var index = i;
                SetResult(index, ShellyStrings.L("… lecture de l'état", "… reading state"));
                tasks.Add(RefreshOneAsync(client, index));
            }

            // Interrogation parallèle des 4 prises : les appels HTTP async s'exécutent en
            // concurrence ; les continuations reviennent sur le thread UI (SynchronizationContext).
            await System.Threading.Tasks.Task.WhenAll(tasks);
        }

        private async System.Threading.Tasks.Task RefreshOneAsync(ShellyClient client, int index)
        {
            var result = await client.TestAsync(_ips[index]);
            _states[index] = result.IsOn;
            _power[index] = result.Ok ? result.PowerW : null;
            SetResult(index, result.Ok ? "✔ " + result.Message : "✖ " + result.Message);
            OnPropertyChanged($"Plug{index}State");
        }

        // ----- Détection réseau -----

        /// <summary>Adresse IP actuellement configurée pour un emplacement (fenêtre de détection).</summary>
        public string GetPlugIpAt(int index) => index >= 0 && index < ShellyOptions.PlugCount ? _ips[index] : null;

        /// <summary>Attribue une IP détectée à un emplacement (sauvegarde immédiate).</summary>
        public void AssignDiscovered(int slot, string ip)
        {
            if (slot < 0 || slot >= ShellyOptions.PlugCount || string.IsNullOrWhiteSpace(ip))
            {
                return;
            }

            _ips[slot] = ip;
            _options.SetPlugIp(slot, ip);
            OnPropertyChanged($"Plug{slot}Ip");
            OnPropertyChanged($"Plug{slot}State");
            StatusMessage = $"Prise {slot + 1} ← {ip}";
        }

        private void ShowDetectionWindow()
        {
            try
            {
                var window = new ShellyDetectionWindow(this)
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                window.ShowDialog();
            }
            catch
            {
                // Ne jamais faire planter NINA si la fenêtre ne peut pas s'ouvrir.
            }
        }

        private static readonly Guid PluginGuid = GetPluginGuid();

        private static Guid GetPluginGuid()
        {
            var attr = typeof(ShellyPowerPlugin).Assembly
                .GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false)
                .OfType<System.Runtime.InteropServices.GuidAttribute>()
                .FirstOrDefault();
            return attr != null && Guid.TryParse(attr.Value, out var g) ? g : Guid.Empty;
        }
    }
}