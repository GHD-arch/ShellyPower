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
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.View.Sequencer;
using System.Windows;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Instruction de séquenceur : arme une EXTINCTION DIFFÉRÉE exécutée PAR LA PRISE
    /// (RPC Gen2+ : Switch.Set on=false&toggle_after=N). La prise bascule seule après le
    /// délai — l'ordinateur qui a envoyé la commande (et exécute cette instruction) peut
    /// s'éteindre lui-même entre-temps, sans être coupé brutalement.
    /// Séquence typique de fin de nuit : [Shelly Power Delayed Off → PC, 120 s] puis
    /// [arrêt du PC] — le PC s'éteint en ~1 min, la prise coupe à 120 s.
    /// Le minuteur tourne dans le matériel de la prise : ni NINA, ni le réseau, ni le PC
    /// ne sont requis à l'échéance.
    /// </summary>
    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Shelly Power Delayed Off")]
    [ExportMetadata("Description", "Extinction différée exécutée par la prise (le PC peut s'éteindre avant).")]
    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerDelayedOffInstruction : ShellyPowerInstructionBase
    {
        protected override string ActionLabel => "Shelly Power Delayed Off";

        private int _delaySeconds = 120;

        /// <summary>Délai d'extinction armé SUR LA PRISE, en secondes.</summary>
        [JsonProperty]
        public int DelaySeconds
        {
            get => _delaySeconds;
            set
            {
                _delaySeconds = Math.Max(0, value);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
            }
        }

        [ImportingConstructor]
        public ShellyPowerDelayedOffInstruction(IProfileService profileService) : base(profileService)
        {
            Category = "Shelly Power";
            Description = ShellyStrings.L(
                "Arme une extinction différée exécutée par la prise (le PC peut s'éteindre avant).",
                "Arms a delayed power-off executed by the plug itself (the PC may shut down first).");
        }

        public ShellyPowerDelayedOffInstruction(ShellyPowerDelayedOffInstruction cloneMe) : base(cloneMe)
        {
        }

        public override object Clone() => new ShellyPowerDelayedOffInstruction(this);

        /// <summary>Nom dynamique incluant le délai : "Shelly Power Delayed Off → PC (120 s)".</summary>
        public new string Name
        {
            get
            {
                try
                {
                    return $"{ActionLabel} → {GetPlugName()} ({DelaySeconds} s)";
                }
                catch
                {
                    return ActionLabel;
                }
            }
        }

        public override bool Validate()
        {
            var baseOk = base.Validate();
            if (DelaySeconds <= 0)
            {
                Issues.Add(ShellyStrings.L(
                    "Le délai doit être supérieur à 0 seconde.",
                    "The delay must be greater than 0 seconds."));
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

            // Prise déjà éteinte : rien à armer (et toggle_after sur un état OFF serait sans effet utile).
            var currentState = await client.GetStatusAsync(ip, true, token);
            if (currentState.Ok && currentState.IsOn == false)
            {
                progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                    $"Shelly '{plugName}' déjà éteinte — extinction différée non armée.",
                    $"Shelly '{plugName}' already off — delayed power-off not armed.") });
                Logger.Info($"Shelly Power Delayed Off: plug '{plugName}' already off.");
                return;
            }

            progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                $"… armement de l'extinction différée de '{plugName}' ({DelaySeconds} s)",
                $"… arming delayed power-off of '{plugName}' ({DelaySeconds} s)") });

            var (armed, detail) = await client.ArmDelayedOffAsync(ip, DelaySeconds, token);
            if (!armed)
            {
                // Surtout pas de coupure immédiate en repli : dangereux pour ce scénario.
                var errorMessage = ShellyStrings.L(
                    $"L'extinction différée n'a pas pu être armée sur '{plugName}' ({ip}). Aucune coupure armée — ne pas éteindre l'ordinateur avant d'avoir résolu ce point.",
                    $"Delayed power-off could not be armed on '{plugName}' ({ip}). Nothing armed — do not shut down the computer before resolving this.");
                Notification.ShowError(errorMessage);
                Logger.Error($"Shelly Power Delayed Off: {errorMessage} — réponse du firmware : {detail ?? "(aucune)"}");
                throw new InvalidOperationException(errorMessage + " [" + (detail ?? "?") + "]");
            }

            var message = ShellyStrings.L(
                $"✔ Extinction différée armée : '{plugName}' ({ip}) s'éteindra toute seule dans {DelaySeconds} s.",
                $"✔ Delayed power-off armed: '{plugName}' ({ip}) will switch itself off in {DelaySeconds} s.");
            Notification.ShowInformation(message);
            Logger.Info("Shelly Power Delayed Off: " + message);
            progress?.Report(new ApplicationStatus { Status = message });
        }

        public override string ToString() => $"Shelly Power Delayed Off: {GetPlugName()} ({DelaySeconds} s)";
    }
}