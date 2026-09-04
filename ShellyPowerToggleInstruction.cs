using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.View.Sequencer;
using System.Windows;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Instruction de séquenceur : bascule une prise Shelly (ON→OFF ou OFF→ON) puis
    /// attend une durée configurée avant de laisser la séquence continuer.
    /// Ex. « allumer 10 min puis éteindre » : ExpectedOn=false, DurationMinutes=10.
    /// À la fin de l'attente, la prise repasse à son état initial (bascule inverse).
    /// </summary>
    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Shelly Power Toggle")]
    [ExportMetadata("Description", "Bascule la prise, attend la durée, puis revient à l'état initial.")]

    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerToggleInstruction : ShellyPowerInstructionBase
    {
        protected override string ActionLabel => "Shelly Power Toggle";

        private int _durationMinutes = 10;

        /// <summary>Durée d'attente entre la bascule et le retour, en minutes.</summary>
        [JsonProperty]
        public int DurationMinutes
        {
            get => _durationMinutes;
            set
            {
                _durationMinutes = Math.Max(0, value);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
            }
        }

        [ImportingConstructor]
        public ShellyPowerToggleInstruction(IProfileService profileService) : base(profileService)
        {
            Category = "Shelly Power";
            Description = ShellyStrings.L("Bascule la prise, attend la durée, puis revient à l'état initial.", "Toggles the plug, waits the duration, then returns to the initial state.");
        }

        public ShellyPowerToggleInstruction(ShellyPowerToggleInstruction cloneMe) : base(cloneMe)
        {
        }

        public override object Clone() => new ShellyPowerToggleInstruction(this);

        public override bool Validate()
        {
            var baseOk = base.Validate();
            if (DurationMinutes <= 0)
            {
                Issues.Add(ShellyStrings.L("La durée doit être supérieure à 0 minute.", "Duration must be greater than 0 minutes."));
            }

            return Issues.Count == 0;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            var ip = GetPlugIp();
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new InvalidOperationException(ShellyStrings.L(
                    $"Prise {SelectedPlugIndex + 1} non configurée (IP manquante).",
                    $"Plug {SelectedPlugIndex + 1} not configured (missing IP)."));
            }

            var plugName = GetPlugName();
            var client = new ShellyClient();

            // 1) Basculer : allumer si éteinte, éteindre si allumée.
            var currentState = await client.GetStatusAsync(ip, true, token);
            var targetOn = !(currentState.IsOn ?? false);

            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"Shelly '{plugName}' : {(targetOn ? "allumage" : "extinction")}...",
                $"Shelly '{plugName}': turning {(targetOn ? "on" : "off")}...") });

            var startState = targetOn
                ? await client.TurnOnAsync(ip, token)
                : await client.TurnOffAsync(ip, token);

            var start = DateTime.UtcNow;
            var duration = TimeSpan.FromMinutes(DurationMinutes);

            // 2) Attendre la durée configurée (annulable, progression affichée).
            while (DateTime.UtcNow - start < duration)
            {
                token.ThrowIfCancellationRequested();
                var remaining = duration - (DateTime.UtcNow - start);
                progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                    $"Shelly '{plugName}' {(targetOn ? "allumée" : "éteinte")} — retour dans {Math.Ceiling(remaining.TotalSeconds)} s",
                    $"Shelly '{plugName}' {(targetOn ? "on" : "off")} — returning in {Math.Ceiling(remaining.TotalSeconds)} s") });
                await Task.Delay(TimeSpan.FromSeconds(2), token);
            }

            // 3) Revenir à l'état initial.
            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"Shelly '{plugName}' : retour à l'état initial...",
                $"Shelly '{plugName}': restoring initial state...") });

            if (startState)
            {
                await client.TurnOnAsync(ip, token);
            }
            else
            {
                await client.TurnOffAsync(ip, token);
            }

            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"Shelly '{plugName}' : état initial restauré.",
                $"Shelly '{plugName}': initial state restored.") });
        }

        public override string ToString() => $"Shelly Power Toggle: {GetPlugName()} ({DurationMinutes} min)";
    }
}