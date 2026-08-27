using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Une prise Shelly exposée à NINA comme un switch pilotable.
    /// Le switch est binaire (0/1). Les accès réseau sont bloquants car l'interface
    /// NINA (Poll / SetValue) est synchrone.
    /// Implémente INotifyPropertyChanged : le template binaire de NINA (onglet Équipement,
    /// toggle « WritableBoolean ») lie Value et TargetValue — sans notification, l'état
    /// affiché ne se mettrait jamais à jour après un Poll/SetValue.
    /// </summary>
    public class ShellySwitch : IWritableSwitch, System.ComponentModel.INotifyPropertyChanged
    {
        private readonly ShellyClient _client;
        private readonly string _ip;
        private readonly bool _protectOff;
        private double _value;
        private double _targetValue;

        public ShellySwitch(short id, string name, string description, string ip, bool protectOff)
        {
            Id = id;
            Name = name;
            Description = description;
            _ip = ip;
            _protectOff = protectOff;
            _client = new ShellyClient();
        }

        public short Id { get; }

        public string Name { get; }

        public string Description { get; }

        /// <summary>État courant : 1 = allumé, 0 = éteint.</summary>
        public double Value
        {
            get => _value;
            private set
            {
                if (_value == value)
                {
                    return;
                }

                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public double Maximum => 1;
        public double Minimum => 0;
        public double StepSize => 1;

        public double TargetValue
        {
            get => _targetValue;
            set
            {
                if (_targetValue == value)
                {
                    return;
                }

                _targetValue = value;
                OnPropertyChanged(nameof(TargetValue));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public bool Poll()
        {
            if (string.IsNullOrWhiteSpace(_ip))
            {
                return false;
            }

            try
            {
                Value = _client.GetIsOnAsync(_ip).GetAwaiter().GetResult() ? 1 : 0;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetValue()
        {
            if (string.IsNullOrWhiteSpace(_ip))
            {
                return;
            }

            // Sécurité « confirmation avant extinction » (actions manuelles depuis NINA) :
            // appelé par le toggle de l'onglet Équipement. Le séquenceur ne passe PAS ici
            // (il appelle ShellyClient directement) — l'automatisation reste inconditionnelle.
            if (TargetValue <= 0 && _protectOff)
            {
                if (!ConfirmPowerOff())
                {
                    // Refus : ramène la consigne sur l'état courant pour que la vérification
                    // de convergence de NINA (SetSwitchValue) se termine sans erreur.
                    TargetValue = Value;
                    return;
                }
            }

            try
            {
                Value = TargetValue > 0
                    ? (_client.TurnOnAsync(_ip).GetAwaiter().GetResult() ? 1 : 0)
                    : (_client.TurnOffAsync(_ip).GetAwaiter().GetResult() ? 1 : 0);
            }
            catch
            {
                // Les erreurs réseau sont silencieuses ici ; l'état reste inchangé.
            }
        }

        /// <summary>
        /// Demande la confirmation d'extinction. NINA appelle SetValue depuis un thread
        /// d'arrière-plan : la MessageBox y serait créée SANS propriétaire et resterait
        /// DERRIÈRE la fenêtre de NINA (un thread secondaire ne peut pas prendre le premier
        /// plan) — invisible, elle bloque tout. On exécute donc la boîte sur le thread UI
        /// avec la fenêtre principale de NINA comme propriétaire (modal, au premier plan).
        /// </summary>
        private bool ConfirmPowerOff()
        {
            var app = Application.Current;
            if (app != null && !app.Dispatcher.CheckAccess())
            {
                var confirmed = false;
                app.Dispatcher.Invoke(() => confirmed = ShowConfirmDialog());
                return confirmed;
            }

            return ShowConfirmDialog();
        }

        private bool ShowConfirmDialog()
        {
            try
            {
                var owner = Application.Current?.MainWindow;
                var answer = MessageBox.Show(
                    owner,
                    $"Éteindre la prise « {Name} » ({_ip}) ?",
                    "Shelly Power — Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);
                return answer == MessageBoxResult.Yes;
            }
            catch
            {
                // Sécurité maximale : si la boîte ne peut pas s'afficher, on n'éteint pas.
                return false;
            }
        }
    }

    /// <summary>
    /// Regroupe les 4 prises configurées en un seul "hub" switch.
    /// Le bouton engrenage "Setup" de l'onglet Équipement ouvre la fenêtre de
    /// configuration (noms + adresses IP + test par prise).
    /// </summary>
    public class ShellySwitchHub : ISwitchHub
    {
        private readonly ICollection<ISwitch> _switches;
        private readonly IProfileService _profileService;

        public ShellySwitchHub(IReadOnlyList<ShellyPlugConfig> plugs, IProfileService profileService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            var list = new List<ISwitch>();
            for (var i = 0; i < ShellyOptions.PlugCount; i++)
            {
                var plug = i < plugs.Count ? plugs[i] : new ShellyPlugConfig();
                var plugName = string.IsNullOrWhiteSpace(plug.Name) ? $"Shelly {i + 1}" : plug.Name;
                list.Add(new ShellySwitch(
                    (short)i,
                    plugName,
                    string.IsNullOrWhiteSpace(plug.Ip) ? "Non configuré — cliquer sur l'engrenage Setup" : plug.Ip,
                    plug.Ip,
                    plug.ProtectOff));

                // Compteur « info only » : puissance instantanée en watts (section
                // Read-only de l'onglet Équipement), ajouté seulement si l'IP est configurée.
                if (!string.IsNullOrWhiteSpace(plug.Ip))
                {
                    list.Add(new ShellyPowerMeter((short)(100 + i), plugName + " (W)", plug.Ip));
                }
            }

            _switches = new ReadOnlyCollection<ISwitch>(list);
        }

        public ICollection<ISwitch> Switches => _switches;

        // ----- Implémentation d'IDevice (requise par ISwitchHub) -----
        public string Name => "Shelly Power";
        public string DisplayName => "Shelly Power";
        public string Id => "NINA.ShellyPower.Hub";
        public string Category => "Power";
        public string Description => "4 prises connectées Shelly";
        public bool Connected => true;
        public bool HasSetupDialog => true;
        public string DriverInfo => "Shelly Power plugin — configurer via l'engrenage Setup";
        public string DriverVersion => "1.0.0";
        public IList<string> SupportedActions => new List<string>();

        public System.Threading.Tasks.Task<bool> Connect(System.Threading.CancellationToken token)
            => System.Threading.Tasks.Task.FromResult(true);

        public void Disconnect() { }

        /// <summary>
        /// Ouvre la fenêtre de configuration (noms, adresses IP, test et pilotage ON/OFF).
        /// Appelée par NINA quand on clique sur l'engrenage "Setup" du device.
        /// BeginInvoke (non bloquant) : jamais d'attente croisée entre threads.
        /// </summary>
        public void SetupDialog()
        {
            var app = Application.Current;
            if (app != null && !app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke((Action)ShowConfigWindow);
            }
            else
            {
                ShowConfigWindow();
            }
        }

        private void ShowConfigWindow()
        {
            try
            {
                var vm = new ShellyOptionsVM(_profileService);
                var window = new Window
                {
                    Title = "Shelly Power — Configuration et pilotage des 4 prises",
                    Content = new ShellyOptionsView { DataContext = vm },
                    Width = 880,
                    MinHeight = 340,
                    SizeToContent = SizeToContent.Height,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                window.ShowDialog();
            }
            catch
            {
                // Ne jamais faire planter NINA si la fenêtre ne peut pas s'ouvrir.
            }
        }

        public string Action(string actionName, string actionParameters) => "";
        public string SendCommandString(string command, bool raw) => "";
        public bool SendCommandBool(string command, bool raw) => false;
        public void SendCommandBlind(string command, bool raw) { }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Fournisseur d'équipement : déclare le hub switch au moteur d'équipements de NINA.
    /// Exporté comme IEquipmentProvider (non-générique) pour être collecté dans
    /// PluginLoader.DeviceProviders, puis distribué par PluginEquipmentProviderManager
    /// vers PluginEquipmentProviders&lt;ISwitchHub&gt; via GetInterfaceType().
    /// Les adresses IP viennent des options du plugin (IProfileService + PluginOptionsAccessor).
    /// </summary>
    [Export(typeof(IEquipmentProvider))]
    public class ShellySwitchProvider : IEquipmentProvider<ISwitchHub>
    {
        [Import]
        public IProfileService ProfileService { get; set; }

        public string Name => "Shelly Power";

        public IList<ISwitchHub> GetEquipment()
        {
            var options = new ShellyOptions(new PluginOptionsAccessor(ProfileService, PluginGuid));
            return new List<ISwitchHub> { new ShellySwitchHub(options.GetPlugs(), ProfileService) };
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