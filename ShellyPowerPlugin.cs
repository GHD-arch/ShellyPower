using System.Threading.Tasks;
using System.ComponentModel.Composition;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Point d'entrée / manifeste du plugin Shelly Power pour NINA 3.x.
    /// Dérive de <see cref="PluginBase"/> ; les métadonnées du manifeste (Name, Version,
    /// MinimumApplicationVersion, ...) sont déclarées dans AssemblyInfo.cs :
    ///   Name   ← AssemblyTitleAttribute      Author ← AssemblyCompanyAttribute
    ///   Version ← AssemblyFileVersionAttribute   Identifier ← GuidAttribute
    ///   License/Tags/MinimumApplicationVersion/LongDescription ← AssemblyMetadataAttribute
    ///
    /// Sur la page Plugins des Options de NINA, NINA affiche un panneau par plugin avec le
    /// MANIFESTE comme DataContext (template trouvé via PluginOptionsDataTemplateSelector,
    /// clé = manifest.Name + "Options"). Ce manifeste expose donc la même logique que le
    /// panneau dockable via <see cref="Core"/> (ShellyPanelCore) — la vue fonctionne
    /// alors de façon identique sur les deux surfaces.
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class ShellyPowerPlugin : PluginBase
    {
        private readonly ShellyPanelCore _core;

        [ImportingConstructor]
        public ShellyPowerPlugin(IProfileService profileService)
        {
            _core = new ShellyPanelCore(profileService);
        }

        /// <summary>Logique partagée (DataContext effectif de la vue sur la page Plugins).</summary>
        public ShellyPanelCore Core => _core;

        public override Task Initialize()
        {
            // Fallback : si le dictionnaire BAML (PluginResources) n'a pas encore été
            // fusionné au moment du rendu, ré-enregistre la vue du panneau (no-op si déjà
            // présent). Non bloquant (BeginInvoke interne).
            ShellyOptionsVM.RegisterViewTemplate();
            return Task.CompletedTask;
        }

        public override Task Teardown() => Task.CompletedTask;
    }
}