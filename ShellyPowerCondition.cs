using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Utility;
using Notification = NINA.Core.Utility.Notification.Notification;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.View.Sequencer;
using System.Windows;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Condition de séquenceur : exécute le bloc d'instructions TANT QUE la prise Shelly
    /// sélectionnée est dans l'état voulu (ON ou OFF). Dès que l'état change (ou que la
    /// prise devient injoignable), le bloc est interrompu proprement.
    /// Utilise un ConditionWatchdog (comme TimeSpanCondition) pour vérifier l'état
    /// périodiquement pendant l'exécution.
    /// </summary>
    [Export(typeof(ISequenceCondition))]
    [ExportMetadata("Name", "Shelly Power While")]
    [ExportMetadata("Description", "Runs the instruction set while the plug is in the wanted state.")]

    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerCondition : SequenceCondition
    {
        private readonly IProfileService _profileService;
        private int _selectedPlugIndex;
        private bool _expectedOn = true;
        private bool _logged;

        [ImportingConstructor]
        public ShellyPowerCondition(IProfileService profileService)
        {
            _profileService = profileService;
            Icon = ShellyIcons.BuildPowerIcon();
            ExpectedOn = true;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenStateWrong, TimeSpan.FromSeconds(3));
        }

        public ShellyPowerCondition(ShellyPowerCondition cloneMe) : this(cloneMe._profileService)
        {
            CopyMetaData(cloneMe);
            SelectedPlugIndex = cloneMe.SelectedPlugIndex;
            ExpectedOn = cloneMe.ExpectedOn;
        }

        public override object Clone() => new ShellyPowerCondition(this);

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

        /// <summary>État qui maintient la condition vraie : true = allumée, false = éteinte.</summary>
        [JsonProperty]
        public bool ExpectedOn
        {
            get => _expectedOn;
            set
            {
                _expectedOn = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ExpectedOnIndex));
                RaisePropertyChanged(nameof(Name));
            }
        }

        /// <summary>Index pour la ComboBox d'état (0 = OFF, 1 = ON).</summary>
        public int ExpectedOnIndex
        {
            get => ExpectedOn ? 1 : 0;
            set => ExpectedOn = value == 1;
        }

        /// <summary>Nom dynamique : "Shelly Power While → Alimentation = ON".</summary>
        public new string Name
        {
            get
            {
                try
                {
                    return $"Shelly Power While → {GetPlugName()} = {(ExpectedOn ? "ON" : "OFF")}";
                }
                catch
                {
                    return "Shelly Power While";
                }
            }
        }

        private string GetPlugIp() => new ShellyOptions(
            new PluginOptionsAccessor(_profileService, PluginGuid)).GetPlugIp(SelectedPlugIndex);

        private string GetPlugName() => new ShellyOptions(
            new PluginOptionsAccessor(_profileService, PluginGuid)).GetPlugName(SelectedPlugIndex);

        private static readonly Guid PluginGuid = GetPluginGuid();

        private static Guid GetPluginGuid()
        {
            var attr = typeof(ShellyPowerPlugin).Assembly
                .GetCustomAttributes(typeof(GuidAttribute), false)
                .OfType<GuidAttribute>()
                .FirstOrDefault();
            return attr != null && Guid.TryParse(attr.Value, out var g) ? g : Guid.Empty;
        }

        /// <summary>Lecture synchrone de l'état de la prise (null si injoignable).</summary>
        private async Task<bool?> ReadStateAsync()
        {
            try
            {
                var ip = GetPlugIp();
                if (string.IsNullOrWhiteSpace(ip))
                {
                    return null;
                }

                var status = await new ShellyClient().GetStatusAsync(ip);
                return status.Ok ? status.IsOn : null;
            }
            catch
            {
                return null;
            }
        }

        private async Task InterruptWhenStateWrong()
        {
            try
            {
                if (Parent == null || !ItemUtility.IsInRootContainer(Parent)
                    || Parent.Status != SequenceEntityStatus.RUNNING
                    || Status == SequenceEntityStatus.DISABLED)
                {
                    return;
                }

                var state = await ReadStateAsync();
                if (state == ExpectedOn)
                {
                    return; // condition toujours vraie
                }

                if (!_logged)
                {
                    _logged = true;
                    var plugName = GetPlugName();
                    var message = ShellyStrings.L(
                        $"Condition Shelly : la prise '{plugName}' n'est plus {(ExpectedOn ? "allumée" : "éteinte")} — interruption du bloc.",
                        $"Shelly condition: plug '{plugName}' is no longer {(ExpectedOn ? "on" : "off")} — interrupting the instruction set.");
                    Logger.Info("Shelly Power While: " + message);
                    Notification.ShowWarning(message);
                }

                Status = SequenceEntityStatus.FINISHED;
                await Parent.Interrupt();
            }
            catch
            {
                // Ne jamais faire planter le watchdog.
            }
        }

        /// <summary>Appelé par le séquenceur entre chaque instruction : true = continuer.</summary>
        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem)
        {
            var state = ReadStateAsync().GetAwaiter().GetResult();
            var ok = state == ExpectedOn;
            if (!ok && !_logged)
            {
                _logged = true;
                Logger.Info(ShellyStrings.L(
                    $"Condition Shelly : la prise '{GetPlugName()}' n'est plus dans l'état attendu — interruption.",
                    $"Shelly condition: plug '{GetPlugName()}' left the expected state — interrupting."));
            }

            return ok;
        }

        public override void SequenceBlockInitialize()
        {
            _logged = false;
            ConditionWatchdog?.Start();
        }

        public override void SequenceBlockTeardown()
        {
            try { ConditionWatchdog?.Cancel(); } catch { }
        }

        public override void ResetProgress()
        {
            Status = SequenceEntityStatus.CREATED;
            _logged = false;
        }

        public override string ToString() => $"Shelly Power While: {GetPlugName()} = {(ExpectedOn ? "ON" : "OFF")}";
    }
}