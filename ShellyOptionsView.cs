using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Vue du panneau Shelly Power (construite en code, sans XAML). Toutes les liaisons
    /// passent par la propriété « Core » du DataContext (ShellyPanelCore) : la vue est donc
    /// fonctionnelle avec le panneau dockable (VM.Core), la fenêtre engrenage (VM.Core) et
    /// la page Plugins des Options (manifeste.Core).
    /// Colonnes : nom de la fonction, adresse IP, Tester, boutons ON/OFF (colorés),
    /// état courant (coloré), résultat du dernier test (coloré).
    /// </summary>
    public class ShellyOptionsView : UserControl
    {
        public ShellyOptionsView()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            var grid = new Grid { Margin = new Thickness(14) };

            // 0: titre, 1: aide, 2: en-têtes de colonnes, 3..6: prises, 7: pied de page
            for (var i = 0; i < 8; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });   // Prise N
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });  // Nom
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });  // IP
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });   // Tester
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });   // ON
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });   // OFF
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });   // État
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(84) });   // Protégée
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Résultat

            // Titre
            var title = new TextBlock
            {
                Text = "Shelly Power — Configuration et pilotage des 4 prises",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(title, 0);
            Grid.SetColumnSpan(title, 9);
            grid.Children.Add(title);

            // Aide
            var help = new TextBlock
            {
                Text = "Saisissez le nom et l'adresse IP de chaque prise (sauvegarde automatique). « Protégée » demande une confirmation avant toute extinction manuelle (le séquenceur n'est pas concerné).",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(help, 1);
            Grid.SetColumnSpan(help, 9);
            grid.Children.Add(help);

            // En-têtes de colonnes
            AddHeader(grid, 2, 1, "Nom de la fonction");
            AddHeader(grid, 2, 2, "Adresse IP");
            AddHeader(grid, 2, 6, "État");
            AddHeader(grid, 2, 7, "Protégée");

            // Lignes de prises
            for (var i = 0; i < ShellyOptions.PlugCount; i++)
            {
                AddPlugRow(grid, 3 + i, i);
            }

            // Pied : Enregistrer + Actualiser + statut
            var save = new Button { Content = "Enregistrer", Padding = new Thickness(12, 4, 12, 4), HorizontalAlignment = HorizontalAlignment.Left };
            save.SetBinding(Button.CommandProperty, new Binding("Core.SaveCommand"));
            Grid.SetRow(save, 7);
            Grid.SetColumn(save, 1);
            grid.Children.Add(save);

            var refresh = new Button { Content = "Actualiser l'état", Padding = new Thickness(10, 4, 12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(8, 0, 0, 0) };
            refresh.SetBinding(Button.CommandProperty, new Binding("Core.RefreshCommand"));
            Grid.SetRow(refresh, 7);
            Grid.SetColumn(refresh, 2);
            grid.Children.Add(refresh);

            var detect = new Button { Content = "Détecter les prises", Padding = new Thickness(10, 4, 12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(8, 0, 0, 0) };
            detect.SetBinding(Button.CommandProperty, new Binding("Core.DetectCommand"));
            Grid.SetRow(detect, 7);
            Grid.SetColumn(detect, 3);
            grid.Children.Add(detect);

            var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            status.SetBinding(TextBlock.TextProperty, new Binding("Core.StatusMessage"));
            Grid.SetRow(status, 7);
            Grid.SetColumn(status, 4);
            Grid.SetColumnSpan(status, 5);
            grid.Children.Add(status);

            Content = grid;
        }

        private static void AddHeader(Grid grid, int row, int column, string text)
        {
            var header = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 6, 4)
            };
            Grid.SetRow(header, row);
            Grid.SetColumn(header, column);
            grid.Children.Add(header);
        }

        private static Button PowerButton(string label, string commandName, string tooltip)
        {
            var button = new Button
            {
                Content = label,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = label == "ON"
                    ? ShellyBrushes.OnBackground
                    : ShellyBrushes.OffBackground,
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(0, 2, 4, 2),
                Padding = new Thickness(6, 3, 6, 3),
                ToolTip = tooltip
            };
            button.SetBinding(Button.CommandProperty, new Binding(commandName));
            return button;
        }

        private static void AddPlugRow(Grid grid, int row, int index)
        {
            var label = new TextBlock
            {
                Text = $"Prise {index + 1}",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetRow(label, row);
            grid.Children.Add(label);

            var name = new TextBox { Margin = new Thickness(0, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center };
            name.SetBinding(TextBox.TextProperty, new Binding($"Core.Plug{index}Name") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            name.ToolTip = "Nom de la fonction (ex. Alimentation, Caméra, Déwattage)";
            Grid.SetRow(name, row);
            Grid.SetColumn(name, 1);
            grid.Children.Add(name);

            var ip = new TextBox { Margin = new Thickness(0, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center };
            ip.SetBinding(TextBox.TextProperty, new Binding($"Core.Plug{index}Ip") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            ip.ToolTip = "Adresse IP de la prise Shelly (ex. 192.168.1.52)";
            Grid.SetRow(ip, row);
            Grid.SetColumn(ip, 2);
            grid.Children.Add(ip);

            var test = new Button { Content = "Tester", Margin = new Thickness(0, 2, 4, 2), Padding = new Thickness(6, 2, 6, 2), CommandParameter = index };
            test.SetBinding(Button.CommandProperty, new Binding("Core.TestCommand"));
            Grid.SetRow(test, row);
            Grid.SetColumn(test, 3);
            grid.Children.Add(test);

            var on = PowerButton("ON", "Core.OnCommand", "Allumer la prise");
            on.CommandParameter = index;
            Grid.SetRow(on, row);
            Grid.SetColumn(on, 4);
            grid.Children.Add(on);

            var off = PowerButton("OFF", "Core.OffCommand", "Éteindre la prise");
            off.CommandParameter = index;
            Grid.SetRow(off, row);
            Grid.SetColumn(off, 5);
            grid.Children.Add(off);

            var state = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            state.SetBinding(TextBlock.TextProperty, new Binding($"Core.Plug{index}State"));
            state.SetBinding(TextBlock.ForegroundProperty, new Binding($"Core.Plug{index}State") { Converter = new StateToBrushConverter() });
            Grid.SetRow(state, row);
            Grid.SetColumn(state, 6);
            grid.Children.Add(state);

            var protect = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                ToolTip = "Si coché, une confirmation est demandée avant d'éteindre cette prise (le séquenceur n'est pas concerné)"
            };
            protect.SetBinding(CheckBox.IsCheckedProperty, new Binding($"Core.Plug{index}ProtectOff") { Mode = BindingMode.TwoWay });
            Grid.SetRow(protect, row);
            Grid.SetColumn(protect, 7);
            grid.Children.Add(protect);

            var result = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            result.SetBinding(TextBlock.TextProperty, new Binding($"Core.Plug{index}Result"));
            result.SetBinding(TextBlock.ForegroundProperty, new Binding($"Core.Plug{index}Result") { Converter = new ResultToBrushConverter() });
            Grid.SetRow(result, row);
            Grid.SetColumn(result, 8);
            grid.Children.Add(result);
        }
    }

    /// <summary>Pinceaux figés du plugin (créés une fois, utilisables depuis tout thread).</summary>
    public static class ShellyBrushes
    {
        public static readonly SolidColorBrush OnBackground = Make(0x2E, 0x7D, 0x32);   // vert foncé
        public static readonly SolidColorBrush OffBackground = Make(0xB7, 0x1C, 0x1C);  // rouge foncé
        public static readonly SolidColorBrush StateOn = Make(0x66, 0xBB, 0x6A);        // vert clair
        public static readonly SolidColorBrush StateOff = Make(0xEF, 0x53, 0x50);       // rouge clair
        public static readonly SolidColorBrush StateUnknown = Make(0xFF, 0xB7, 0x4D);   // orange
        public static readonly SolidColorBrush Neutral = Make(0x9E, 0x9E, 0x9E);        // gris
        public static readonly SolidColorBrush Ok = StateOn;
        public static readonly SolidColorBrush Err = StateOff;

        private static SolidColorBrush Make(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }

    /// <summary>État de la prise -> couleur (vert allumée, rouge éteinte, orange inconnu, gris —).</summary>
    public class StateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? "";
            if (text.Contains("allumée"))
            {
                return ShellyBrushes.StateOn;
            }

            if (text.Contains("éteinte"))
            {
                return ShellyBrushes.StateOff;
            }

            if (text.Contains("inconnu"))
            {
                return ShellyBrushes.StateUnknown;
            }

            return ShellyBrushes.Neutral;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>Résultat de test -> couleur (vert ✔, rouge ✖, sinon gris).</summary>
    public class ResultToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? "";
            if (text.StartsWith("✔", StringComparison.Ordinal))
            {
                return ShellyBrushes.Ok;
            }

            if (text.StartsWith("✖", StringComparison.Ordinal))
            {
                return ShellyBrushes.Err;
            }

            return ShellyBrushes.Neutral;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}