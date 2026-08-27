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

        static ShellyPowerInstructionBase()
        {
            // Injecte les templates d'edition dans les Resources du ContentPresenter au
            // moment de son chargement. Scope element : le ContentPresenter trouve le
            // template (ComboBox affichee) mais l'ItemsControl du sequencer ne le trouve
            // PAS (la ligne complete avec ses boutons d'action est preservee).
            System.Windows.EventManager.RegisterClassHandler(
                typeof(System.Windows.Controls.ContentPresenter),
                System.Windows.FrameworkElement.LoadedEvent,
                new System.Windows.RoutedEventHandler(OnContentPresenterLoaded));
        }

        [ImportingConstructor]
        protected ShellyPowerInstructionBase(IProfileService profileService)
        {
            _profileService = profileService;
            Icon = BuildIcon();
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
                Issues.Add($"Prise {SelectedPlugIndex + 1} : aucune adresse IP configurée.");
                return false;
            }

            return true;
        }

        private static System.Windows.Media.GeometryGroup BuildIcon()
        {
            // Icône simple (rectangles) pour l'arbre de séquence. Figée (Freeze) car la classe
            // est instanciée par MEF sur un thread d'arrière-plan : un Freezable non figé
            // provoquerait une exception d'affinité de thread au rendu WPF.
            var group = new System.Windows.Media.GeometryGroup();
            group.Children.Add(new System.Windows.Media.RectangleGeometry(
                new System.Windows.Rect(0, 0, 16, 8)));
            group.Children.Add(new System.Windows.Media.RectangleGeometry(
                new System.Windows.Rect(4, 8, 8, 8)));
            group.Freeze();
            return group;
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

        // ----- Injection du template d'edition dans le ContentPresenter -----
        // Le ContentPresenter "SequenceItemContent" du SequenceBlockView de NINA utilise le
        // lookup implicite par DataType pour afficher l'editeur. Un template DataType dans
        // Application.Resources serait aussi trouve par l'ItemsControl (remplaçant toute la
        // ligne + boutons). En l'injectant dans les Resources du ContentPresenter individuel,
        // il n'est visible qu'a cet element — l'editeur s'affiche, les boutons restent.

        private static void OnContentPresenterLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.ContentPresenter cp))
            {
                return;
            }

            var content = cp.Content;
            if (content == null)
            {
                return;
            }

            var type = content.GetType();
            object key = null;
            System.Windows.DataTemplate template = null;

            if (type == typeof(ShellyPowerOnInstruction) && !cp.Resources.Contains(typeof(ShellyPowerOnInstruction)))
            {
                key = typeof(ShellyPowerOnInstruction);
                template = BuildEditorTemplate("ON", "#FF66BB6A");
            }
            else if (type == typeof(ShellyPowerOffInstruction) && !cp.Resources.Contains(typeof(ShellyPowerOffInstruction)))
            {
                key = typeof(ShellyPowerOffInstruction);
                template = BuildEditorTemplate("OFF", "#FFEF5350");
            }

            if (key != null && template != null)
            {
                cp.Resources[key] = template;
                // Force le ContentPresenter a réévaluer son template : sans ça, le template
                // est injecté trop tard (après la première résolution) et la ComboBox
                // n'apparaît pas.
                cp.Content = null;
                cp.Content = content;
            }
        }

        private static System.Windows.DataTemplate BuildEditorTemplate(string label, string color)
        {
            var template = new System.Windows.DataTemplate();
            template.DataType = label == "ON" ? typeof(ShellyPowerOnInstruction) : typeof(ShellyPowerOffInstruction);

            var panel = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
            panel.SetValue(System.Windows.Controls.StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
            panel.SetValue(System.Windows.FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Left);

            var lbl = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            lbl.SetValue(System.Windows.Controls.TextBlock.TextProperty, label);
            lbl.SetValue(System.Windows.Controls.TextBlock.FontWeightProperty, System.Windows.FontWeights.Bold);
            lbl.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)));
            lbl.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            lbl.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new System.Windows.Thickness(0, 0, 6, 0));
            panel.AppendChild(lbl);

            var combo = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ComboBox));
            combo.SetValue(System.Windows.Controls.ComboBox.MinWidthProperty, 120.0);
            combo.SetValue(System.Windows.Controls.ComboBox.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            combo.SetValue(System.Windows.FrameworkElement.ToolTipProperty, "Prise à piloter");
            combo.SetBinding(System.Windows.Controls.ComboBox.ItemsSourceProperty, new System.Windows.Data.Binding("PlugNames"));
            combo.SetBinding(System.Windows.Controls.ComboBox.SelectedIndexProperty,
                new System.Windows.Data.Binding("SelectedPlugIndex") { Mode = System.Windows.Data.BindingMode.TwoWay });
            panel.AppendChild(combo);

            template.VisualTree = panel;
            return template;
        }
    }

    /// <summary>Instruction de séquenceur : allumer une prise Shelly.</summary>
    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Shelly Power On")]
    [ExportMetadata("Description", "Allume une prise Shelly configurée.")]
    [ExportMetadata("Icon", "ButtonSVG")]
    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerOnInstruction : ShellyPowerInstructionBase
    {
        protected override string ActionLabel => "Shelly Power On";

        [ImportingConstructor]
        public ShellyPowerOnInstruction(IProfileService profileService) : base(profileService)
        {
            Category = "Shelly Power";
            Description = "Allume une prise Shelly configurée.";
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
                throw new InvalidOperationException($"Prise {SelectedPlugIndex + 1} non configurée (IP manquante).");
            }

            progress?.Report(new ApplicationStatus { Status = $"Allumage Shelly '{GetPlugName()}' ({ip})..." });
            var client = new ShellyClient();
            await client.TurnOnAsync(ip, token);
            progress?.Report(new ApplicationStatus { Status = $"Shelly '{GetPlugName()}' allumée." });
        }

        public override string ToString() => $"Shelly Power On: {GetPlugName()}";
    }

    /// <summary>Instruction de séquenceur : éteindre une prise Shelly.</summary>
    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Shelly Power Off")]
    [ExportMetadata("Description", "Éteint une prise Shelly configurée.")]
    [ExportMetadata("Icon", "ButtonSVG")]
    [ExportMetadata("Category", "Shelly Power")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ShellyPowerOffInstruction : ShellyPowerInstructionBase
    {
        protected override string ActionLabel => "Shelly Power Off";

        [ImportingConstructor]
        public ShellyPowerOffInstruction(IProfileService profileService) : base(profileService)
        {
            Category = "Shelly Power";
            Description = "Éteint une prise Shelly configurée.";
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
                throw new InvalidOperationException($"Prise {SelectedPlugIndex + 1} non configurée (IP manquante).");
            }

            progress?.Report(new ApplicationStatus { Status = $"Extinction Shelly '{GetPlugName()}' ({ip})..." });
            var client = new ShellyClient();
            await client.TurnOffAsync(ip, token);
            progress?.Report(new ApplicationStatus { Status = $"Shelly '{GetPlugName()}' éteinte." });
        }

        public override string ToString() => $"Shelly Power Off: {GetPlugName()}";
    }

}
