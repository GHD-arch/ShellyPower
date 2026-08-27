using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Panneau dockable « Shelly Power » de la fenêtre principale.
    /// Toute la logique (propriétés des 4 prises + commandes) vit dans
    /// <see cref="ShellyPanelCore"/>, exposé via la propriété <see cref="Core"/> :
    /// la même vue <see cref="ShellyOptionsView"/> fonctionne ainsi partout
    /// (dock, fenêtre engrenage, page Plugins des Options).
    /// </summary>
    [Export(typeof(IDockableVM))]
    [ExportMetadata("Name", "Shelly Power")]
    public class ShellyOptionsVM : DockableVM
    {
        private readonly ShellyPanelCore _core;

        static ShellyOptionsVM()
        {
            RegisterViewTemplate();
        }

        /// <summary>
        /// Associe ce VM à sa vue <see cref="ShellyOptionsView"/> dans les ressources de
        /// l'application, sous les clés attendues par les sélecteurs de NINA :
        ///   - GenericTemplateSelector (docking)  : GetType().FullName + "_Dockable"
        ///   - PluginOptionsDataTemplateSelector  : manifest.Name + "_Options"/"Options"
        /// Points d'affinité de thread (les deux ont causé des bugs réels) :
        ///   1. le DataTemplate est un DispatcherObject : il doit être CRÉÉ sur le thread UI
        ///      (sinon le rendu plante) ;
        ///   2. l'enregistrement NE DOIT JAMAIS BLOQUER le thread appelant : un
        ///      Dispatcher.Invoke depuis le thread de chargement des plugins fige NINA au
        ///      lancement (interblocage). On utilise donc BeginInvoke (asynchrone).
        /// Les clés sont posées directement dans Application.Current.Resources (pas dans un
        /// dictionnaire fusionné : NINA scelle les valeurs des dictionnaires fusionnés depuis
        /// son thread de chargement, ce qui lève « The calling thread cannot access... »).
        /// </summary>
        internal static void RegisterViewTemplate()
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            if (!app.Dispatcher.CheckAccess())
            {
                // Non bloquant : jamais d'Invoke depuis le chargement des plugins.
                app.Dispatcher.BeginInvoke((Action)RegisterNow);
                return;
            }

            RegisterNow();
        }

        private static void RegisterNow()
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            var keyDockable = typeof(ShellyOptionsVM).FullName + "_Dockable";
            if (app.Resources.Contains(keyDockable))
            {
                return; // déjà enregistré
            }

            var template = new DataTemplate(typeof(ShellyOptionsVM));
            template.VisualTree = new FrameworkElementFactory(typeof(ShellyOptionsView));

            app.Resources[keyDockable] = template;
            app.Resources["Shelly Power" + "_Options"] = template;
            app.Resources["Shelly Power" + "Options"] = template;
            app.Resources[typeof(ShellyOptionsVM).Name + "_Options"] = template;
            app.Resources[typeof(ShellyOptionsVM).FullName + "_Options"] = template;
        }

        [ImportingConstructor]
        public ShellyOptionsVM(IProfileService profileService) : base(profileService)
        {
            _core = new ShellyPanelCore(profileService);
            Title = "Shelly Power";
            // Géométrie figée (Freeze) : le VM est créé par MEF sur un thread d'arrière-plan ;
            // un Freezable non figé provoquerait « Must create DependencySource on same
            // Thread » au rendu WPF.
            var image = new GeometryGroup();
            image.Children.Add(new RectangleGeometry(new Rect(0, 0, 16, 16)));
            image.Freeze();
            ImageGeometry = image;
        }

        /// <summary>Logique partagée (DataContext effectif de la vue).</summary>
        public ShellyPanelCore Core => _core;

        // AvalonDock : identifiant stable du panneau (persistance de la disposition)
        // et classement dans les "Outils" du dock.
        public override string ContentId => "NINA.ShellyPower.OptionsPanel";
        public override bool IsTool => true;
    }

    /// <summary>Commande synchrone simple.</summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
    }

    /// <summary>Commande asynchrone simple.</summary>
    public class RelayCommandAsync : ICommand
    {
        private readonly Func<int, System.Threading.Tasks.Task> _execute;

        public RelayCommandAsync(Func<int, System.Threading.Tasks.Task> execute) => _execute = execute;

        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;
        public async void Execute(object parameter)
        {
            var index = parameter is int i ? i : 0;
            await _execute(index);
        }
    }
}