using NINA.Profile;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Base commune aux instructions de séquenceur du plugin. Gère le choix de la prise
    /// (parmi les 4 configurées) et l'accès aux options (IP/nom).
    /// Le Nom de l'instruction est dynamique : "Shelly Power On → Alimentation" — il
    /// identifie immédiatement l'action et la prise ciblée dans l'arbre du séquenceur,
    /// sans nécessiter de template personnalisé (qui interfere avec les boutons d'action
    /// de NINA sur le côté droit de la ligne).
    /// </summary>
    public abstract class ShellyPowerInstructionBase : SequenceItem, IValidatable
    {
        private readonly IProfileService _profileService;
        private int _selectedPlugIndex;

        /// <summary>Libellé d'action ("Shelly Power On" / "Shelly Power Off").</summary>
        protected abstract string ActionLabel { get; }

        [ImportingConstructor]
        protected ShellyPowerInstructionBase(IProfileService profileService)
        {
            _profileService = profileService;
            Icon = ShellyIcons.BuildPowerIcon();
            SelectedPlugIndex = 0;
        }

        protected ShellyPowerInstructionBase(ShellyPowerInstructionBase cloneMe)
            : base(cloneMe)
        {
            _profileService = cloneMe._profileService;
            SelectedPlugIndex = cloneMe.SelectedPlugIndex;
        }

        [JsonProperty]
        public int SelectedPlugIndex
        {
            get => _selectedPlugIndex;
            set
            {
                if (_selectedPlugIndex == value)
                {
                    return;
                }

                _selectedPlugIndex = value;
                OnPropertyChanged(nameof(SelectedPlugIndex));
                OnPropertyChanged(nameof(Name)); // force le rafraichissement du Nom affiche
            }
        }

        /// <summary>
        /// Nom dynamique affiché dans le séquenceur : "Shelly Power On → Alimentation".
        /// Calculé à chaque lecture (pas de stockage) pour rester synchro avec la prise
        /// sélectionnée. Utilise 'new' (Name n'est pas virtual sur SequenceItem) — WPF
        /// résout le binding par type runtime (notre instruction), donc cette version
        /// est affichée dans l'arbre du séquenceur.
        /// </summary>
        public new string Name
        {
            get
            {
                try
                {
                    return _profileService != null
                        ? $"{ActionLabel} → {GetPlugName()}"
                        : ActionLabel;
                }
                catch
                {
                    return ActionLabel;
                }
            }
        }

        /// <summary>Noms des prises configurées (avec IP), pour le menu déroulant.</summary>
        public IReadOnlyList<string> PlugNames => new ShellyOptions(
            new PluginOptionsAccessor(_profileService, PluginGuid)).GetAvailablePlugNames();

        protected string GetPlugIp() => new ShellyOptions(
            new PluginOptionsAccessor(_profileService, PluginGuid)).GetPlugIp(SelectedPlugIndex);

        protected string GetPlugName() => new ShellyOptions(
            new PluginOptionsAccessor(_profileService, PluginGuid)).GetPlugName(SelectedPlugIndex);

        protected IProfileService ProfileService => _profileService;

        public IList<string> Issues { get; } = new List<string>();

        public virtual bool Validate()
        {
            Issues.Clear();
            if (string.IsNullOrWhiteSpace(GetPlugIp()))
            {
                Issues.Add(ShellyStrings.L(
                    $"Prise {SelectedPlugIndex + 1} : aucune adresse IP configurée.",
                    $"Plug {SelectedPlugIndex + 1}: no IP address configured."));
                return false;
            }

            return true;
        }


        private static readonly Guid PluginGuid = GetPluginGuid();

        private static Guid GetPluginGuid()
        {
            var attr = typeof(ShellyPowerPlugin).Assembly
                .GetCustomAttributes(typeof(GuidAttribute), false)
                .OfType<GuidAttribute>()
                .FirstOrDefault();
            return attr != null && Guid.TryParse(attr.Value, out var g) ? g : Guid.Empty;
        }


    }

    /// <summary>Instruction de séquenceur : allumer une prise Shelly.</summary>
    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Shelly Power On")]
    [ExportMetadata("Description", "Turns on a configured Shelly plug.")]

    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerOnInstruction : ShellyPowerInstructionBase
    {
        protected override string ActionLabel => "Shelly Power On";

        [ImportingConstructor]
        public ShellyPowerOnInstruction(IProfileService profileService) : base(profileService)
        {
            Category = "Shelly Power";
            Description = ShellyStrings.L("Allume une prise Shelly configurée.", "Turns on a configured Shelly plug.");
        }

        public ShellyPowerOnInstruction(ShellyPowerOnInstruction cloneMe) : base(cloneMe)
        {
        }

        public override object Clone() => new ShellyPowerOnInstruction(this);

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            var ip = GetPlugIp();
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new InvalidOperationException(ShellyStrings.L(
                    $"Prise {SelectedPlugIndex + 1} non configurée (IP manquante).",
                    $"Plug {SelectedPlugIndex + 1} not configured (missing IP)."));
            }

            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"Allumage Shelly '{GetPlugName()}' ({ip})...",
                $"Turning Shelly '{GetPlugName()}' on ({ip})...") });
            var client = new ShellyClient();
            await client.TurnOnAsync(ip, token);
            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"Shelly '{GetPlugName()}' allumée.",
                $"Shelly '{GetPlugName()}' turned on.") });
        }

        public override string ToString() => $"Shelly Power On: {GetPlugName()}";
    }

    /// <summary>Instruction de séquenceur : éteindre une prise Shelly.</summary>
    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Shelly Power Off")]
    [ExportMetadata("Description", "Turns off a configured Shelly plug.")]

    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerOffInstruction : ShellyPowerInstructionBase
    {
        protected override string ActionLabel => "Shelly Power Off";

        [ImportingConstructor]
        public ShellyPowerOffInstruction(IProfileService profileService) : base(profileService)
        {
            Category = "Shelly Power";
            Description = ShellyStrings.L("Éteint une prise Shelly configurée.", "Turns off a configured Shelly plug.");
        }

        public ShellyPowerOffInstruction(ShellyPowerOffInstruction cloneMe) : base(cloneMe)
        {
        }

        public override object Clone() => new ShellyPowerOffInstruction(this);

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            var ip = GetPlugIp();
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new InvalidOperationException(ShellyStrings.L(
                    $"Prise {SelectedPlugIndex + 1} non configurée (IP manquante).",
                    $"Plug {SelectedPlugIndex + 1} not configured (missing IP)."));
            }

            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"Extinction Shelly '{GetPlugName()}' ({ip})...",
                $"Turning Shelly '{GetPlugName()}' off ({ip})...") });
            var client = new ShellyClient();
            await client.TurnOffAsync(ip, token);
            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"Shelly '{GetPlugName()}' éteinte.",
                $"Shelly '{GetPlugName()}' turned off.") });
        }

        public override string ToString() => $"Shelly Power Off: {GetPlugName()}";
    }

}
