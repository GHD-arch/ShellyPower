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
    /// Instruction de séquenceur : attend qu'une prise Shelly soit dans l'état voulu
    /// (ON ou OFF), avec un délai maximum. Si le délai est dépassé sans que l'état
    /// soit atteint, l'instruction échoue (la séquence s'arrête ou applique le
    /// comportement d'erreur choisi par l'utilisateur).
    /// Utile pour vérifier qu'un matériel alimenté est réellement prêt (ou éteint)
    /// avant de poursuivre, et pour détecter une prise injoignable avant d'imager.
    /// </summary>
    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Shelly Power Wait")]
    [ExportMetadata("Description", "Attendre que la prise soit ON ou OFF (avec délai max).")]
    [ExportMetadata("Icon", "HourglassSVG")]
    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerWaitInstruction : ShellyPowerInstructionBase
    {
        protected override string ActionLabel => "Shelly Power Wait";
        /// <summary>État attendu : true = allumée, false = éteinte.</summary>
        [JsonProperty]
        public bool ExpectedOn { get; set; }

        /// <summary>Index pour la ComboBox d'état (0 = OFF, 1 = ON).</summary>
        public int ExpectedOnIndex
        {
            get => ExpectedOn ? 1 : 0;
            set => ExpectedOn = value == 1;
        }

        /// <summary>Délai maximum d'attente, en secondes.</summary>
        [JsonProperty]
        public int TimeoutSeconds { get; set; } = 60;

        [ImportingConstructor]
        public ShellyPowerWaitInstruction(IProfileService profileService) : base(profileService)
        {
            Category = "Shelly Power";
            Description = ShellyStrings.L("Attend que la prise soit dans l'état voulu (avec délai max).", "Waits until the plug reaches the wanted state (with timeout).");
            ExpectedOn = true;
        }

        public ShellyPowerWaitInstruction(ShellyPowerWaitInstruction cloneMe) : base(cloneMe)
        {
            ExpectedOn = cloneMe.ExpectedOn;
            TimeoutSeconds = cloneMe.TimeoutSeconds;
        }

        public override object Clone() => new ShellyPowerWaitInstruction(this);

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
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(Math.Max(5, TimeoutSeconds));
            var client = new ShellyClient();

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var status = await client.GetStatusAsync(ip, true, token);
                if (status.Ok && status.IsOn == ExpectedOn)
                {
                    progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                        $"Shelly '{plugName}' : état confirmé ({(ExpectedOn ? "ON" : "OFF")}).",
                        $"Shelly '{plugName}': state confirmed ({(ExpectedOn ? "ON" : "OFF")}).") });
                    return;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    var errorMessage = ShellyStrings.L(
                        $"Délai dépassé : la prise '{plugName}' ({ip}) n'est pas {(ExpectedOn ? "allumée" : "éteinte")} après {TimeoutSeconds} s.",
                        $"Timeout: plug '{plugName}' ({ip}) did not reach {(ExpectedOn ? "ON" : "OFF")} within {TimeoutSeconds} s.");

                    // Notification visible + log : NINA avale sinon l'exception en statut SKIP.
                    NINA.Core.Utility.Notification.Notification.ShowError(errorMessage);
                    NINA.Core.Utility.Logger.Error("Shelly Power Wait: " + errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                progress?.Report(new ApplicationStatus { Status = ShellyStrings.L(
                    $"… attente de '{plugName}' = {(ExpectedOn ? "ON" : "OFF")} (reste {Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds)} s)",
                    $"… waiting for '{plugName}' = {(ExpectedOn ? "ON" : "OFF")} ({Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds)} s left)") });

                await Task.Delay(2000, token);
            }
        }

        public override string ToString() => $"Shelly Power Wait: {GetPlugName()} = {(ExpectedOn ? "ON" : "OFF")}";
    }
}