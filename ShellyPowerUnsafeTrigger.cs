using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.View.Sequencer;
using System.Windows;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Déclencheur de sécurité : quand le safety monitor passe en UNSAFE (fin du
    /// safe-to-observe — nuages, pluie, couvercle ouvert…), la prise Shelly sélectionnée
    /// est automatiquement ÉTEINTE. Les instructions imbriquées dans le déclencheur
    /// s'exécutent ensuite (TriggerRunner standard de NINA).
    /// </summary>
    [Export(typeof(ISequenceTrigger))]
    [ExportMetadata("Name", "Shelly Power Unsafe")]
    [ExportMetadata("Description", "Coupe la prise sélectionnée quand le safety monitor devient UNSAFE.")]

    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerUnsafeTrigger : SequenceTrigger, IValidatable
    {
        private readonly ISafetyMonitorMediator _safetyMonitorMediator;
        private readonly IProfileService _profileService;
        private int _selectedPlugIndex;
        private bool _triggered;
        private bool _hasSeenConnected;
        private DateTime _lastNotify = DateTime.MinValue;

        [ImportingConstructor]
        public ShellyPowerUnsafeTrigger(ISafetyMonitorMediator safetyMonitorMediator, IProfileService profileService)
        {
            _safetyMonitorMediator = safetyMonitorMediator;
            _profileService = profileService;
            Icon = ShellyIcons.BuildPowerIcon();
        }

        public ShellyPowerUnsafeTrigger(ShellyPowerUnsafeTrigger cloneMe)
            : this(cloneMe._safetyMonitorMediator, cloneMe._profileService)
        {
            CopyMetaData(cloneMe);
            SelectedPlugIndex = cloneMe.SelectedPlugIndex;
        }

        public override object Clone() => new ShellyPowerUnsafeTrigger(this);

        private static readonly Guid PluginGuid = GetPluginGuid();

        private static Guid GetPluginGuid()
        {
            var attr = typeof(ShellyPowerPlugin).Assembly
                .GetCustomAttributes(typeof(GuidAttribute), false)
                .OfType<GuidAttribute>()
                .FirstOrDefault();
            return attr != null && Guid.TryParse(attr.Value, out var g) ? g : Guid.Empty;
        }

        private string GetPlugIp() => new ShellyOptions(
            new PluginOptionsAccessor(_profileService, PluginGuid)).GetPlugIp(SelectedPlugIndex);

        private string GetPlugName() => new ShellyOptions(
            new PluginOptionsAccessor(_profileService, PluginGuid)).GetPlugName(SelectedPlugIndex);

        [JsonProperty]
        public int SelectedPlugIndex
        {
            get => _selectedPlugIndex;
            set
            {
                _selectedPlugIndex = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
            }
        }

        /// <summary>Nom dynamique : "Shelly Power Unsafe → Alimentation OFF".</summary>
        public new string Name
        {
            get
            {
                try
                {
                    return $"Shelly Power Unsafe → {GetPlugName()} OFF";
                }
                catch
                {
                    return "Shelly Power Unsafe";
                }
            }
        }

        private bool IsConnectedSafe()
        {
            var info = _safetyMonitorMediator.GetInfo();
            return info.Connected && info.IsSafe;
        }

        /// <summary>Déclenché quand le safety monitor est connecté et UNSAFE.</summary>
        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            var info = _safetyMonitorMediator.GetInfo();
            if (!info.Connected)
            {
                return false;
            }

            _triggered = true;
            return !info.IsSafe;
        }

        public override bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            var info = _safetyMonitorMediator.GetInfo();
            return info.Connected && !info.IsSafe;
        }

        /// <summary>
        /// Coupe la prise sélectionnée si elle est allumée. NINA appelle Execute tant que
        /// le safety monitor est UNSAFE : les notifications sont limitées à 1/minute.
        /// </summary>
        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            var ip = GetPlugIp();
            var plugName = GetPlugName();
            if (string.IsNullOrWhiteSpace(ip))
            {
                if ((DateTime.UtcNow - _lastNotify).TotalMinutes >= 1)
                {
                    _lastNotify = DateTime.UtcNow;
                    Notification.ShowWarning(ShellyStrings.L(
                        "Shelly Power Unsafe : aucune adresse IP configurée pour la prise à couper.",
                        "Shelly Power Unsafe: no IP address configured for the plug to switch off."));
                }
                return;
            }

            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"⚠ UNSAFE — vérification de la prise '{plugName}' ({ip})...",
                $"⚠ UNSAFE — checking plug '{plugName}' ({ip})...") });

            try
            {
                var client = new ShellyClient();
                var status = await client.GetStatusAsync(ip, true, token);
                if (!status.Ok)
                {
                    if ((DateTime.UtcNow - _lastNotify).TotalMinutes >= 1)
                    {
                        _lastNotify = DateTime.UtcNow;
                        Notification.ShowError(ShellyStrings.L(
                            $"⚠ UNSAFE : la prise '{plugName}' ({ip}) est injoignable — impossible de confirmer la coupure.",
                            $"⚠ UNSAFE: plug '{plugName}' ({ip}) is unreachable — power-off could not be confirmed."));
                    }

                    return;
                }

                if (status.IsOn == true)
                {
                    await client.TurnOffAsync(ip, token);
                    _lastNotify = DateTime.MinValue;
                    var message = ShellyStrings.L(
                        $"⚠ UNSAFE : la prise '{plugName}' ({ip}) a été coupée automatiquement.",
                        $"⚠ UNSAFE: plug '{plugName}' ({ip}) has been switched off automatically.");
                    Notification.ShowWarning(message);
                    Logger.Info("Shelly Power Unsafe: " + message);
                }
                else if ((DateTime.UtcNow - _lastNotify).TotalMinutes >= 1)
                {
                    _lastNotify = DateTime.UtcNow;
                    Notification.ShowInformation(ShellyStrings.L(
                        $"⚠ UNSAFE : la prise '{plugName}' était déjà éteinte.",
                        $"⚠ UNSAFE: plug '{plugName}' was already off."));
                    Logger.Info($"Shelly Power Unsafe: plug '{plugName}' already off.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Shelly Power Unsafe: échec de la coupure — " + ex.Message);
            }
        }

        public IList<string> Issues { get; } = new List<string>();

        public bool Validate()
        {
            Issues.Clear();
            if (string.IsNullOrWhiteSpace(GetPlugIp()))
            {
                Issues.Add(ShellyStrings.L(
                    $"Prise {SelectedPlugIndex + 1} : aucune adresse IP configurée.",
                    $"Plug {SelectedPlugIndex + 1}: no IP address configured."));
            }

            return Issues.Count == 0;
        }

        public override string ToString() => $"Trigger: Shelly Power Unsafe ({GetPlugName()})";
    }
}