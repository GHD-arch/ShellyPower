using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Vue du panneau Shelly Power (construite en code, sans XAML). Colonnes : nom de la
    /// fonction, adresse IP, boutons ON/OFF colorés, état, protégée, résultat.
    /// Libellés bilingues : détecte la culture UI (français par défaut, anglais si en-US/en-GB).
    /// </summary>
    public class ShellyOptionsView : UserControl
    {
        // Libellés bilingues : anglais par défaut (en-US/en-GB), français si culture UI "fr".
        private static readonly bool IsEnglish =
            System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";

        private static string L(string fr, string en) => IsEnglish ? en : fr;

        public ShellyOptionsView()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            var grid = new Grid { Margin = new Thickness(14) };

            for (var i = 0; i < 8; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(84) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock
            {
                Text = L("Shelly Power — Configuration et pilotage des 4 prises",
                          "Shelly Power — Configuration and control of 4 plugs"),
                FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(title, 0); Grid.SetColumnSpan(title, 9); grid.Children.Add(title);

            var help = new TextBlock
            {
                Text = L("Saisissez le nom et l'adresse IP de chaque prise (sauvegarde automatique). « Protégée » demande une confirmation avant toute extinction manuelle.",
                          "Enter the name and IP address of each plug (auto-saved). « Protected » asks for confirmation before turning off manually."),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(help, 1); Grid.SetColumnSpan(help, 9); grid.Children.Add(help);

            AddHeader(grid, 2, 1, L("Nom de la fonction", "Function name"));
            AddHeader(grid, 2, 2, L("Adresse IP", "IP address"));
            AddHeader(grid, 2, 6, L("État", "State"));
            AddHeader(grid, 2, 7, L("Protégée", "Protected"));

            for (var i = 0; i < ShellyOptions.PlugCount; i++)
                AddPlugRow(grid, 3 + i, i);

            // Pied de page : boutons dans un panneau horizontal pleine largeur (les colonnes
            // de données sont trop étroites pour les libellés anglais, ex. "Detect plugs").
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var save = new Button { Content = L("Enregistrer", "Save"), Padding = new Thickness(12, 4, 12, 4) };
            save.SetBinding(Button.CommandProperty, new Binding("Core.SaveCommand"));
            footer.Children.Add(save);

            var refresh = new Button { Content = L("Actualiser l'état", "Refresh state"), Padding = new Thickness(10, 4, 12, 4), Margin = new Thickness(8, 0, 0, 0) };
            refresh.SetBinding(Button.CommandProperty, new Binding("Core.RefreshCommand"));
            footer.Children.Add(refresh);

            var detect = new Button { Content = L("Détecter les prises", "Detect plugs"), Padding = new Thickness(10, 4, 12, 4), Margin = new Thickness(8, 0, 0, 0) };
            detect.SetBinding(Button.CommandProperty, new Binding("Core.DetectCommand"));
            footer.Children.Add(detect);

            var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            status.SetBinding(TextBlock.TextProperty, new Binding("Core.StatusMessage"));
            footer.Children.Add(status);

            Grid.SetRow(footer, 7);
            Grid.SetColumn(footer, 0);
            Grid.SetColumnSpan(footer, 9);
            grid.Children.Add(footer);

            Content = grid;
        }

        private static void AddHeader(Grid grid, int row, int column, string text)
        {
            var header = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 6, 4) };
            Grid.SetRow(header, row); Grid.SetColumn(header, column); grid.Children.Add(header);
        }

        private static void AddPlugRow(Grid grid, int row, int index)
        {
            var label = new TextBlock
            {
                Text = L($"Prise {index + 1}", $"Plug {index + 1}"),
                VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetRow(label, row); grid.Children.Add(label);

            var name = new TextBox { Margin = new Thickness(0, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center };
            name.SetBinding(TextBox.TextProperty, new Binding($"Core.Plug{index}Name") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            name.ToolTip = L("Nom de la fonction (ex. Alimentation)", "Function name (e.g. Power)");
            Grid.SetRow(name, row); Grid.SetColumn(name, 1); grid.Children.Add(name);

            var ip = new TextBox { Margin = new Thickness(0, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center };
            ip.SetBinding(TextBox.TextProperty, new Binding($"Core.Plug{index}Ip") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            ip.ToolTip = L("Adresse IP de la prise Shelly", "Shelly plug IP address");
            Grid.SetRow(ip, row); Grid.SetColumn(ip, 2); grid.Children.Add(ip);

            var test = new Button { Content = L("Tester", "Test"), Margin = new Thickness(0, 2, 4, 2), Padding = new Thickness(6, 2, 6, 2), CommandParameter = index };
            test.SetBinding(Button.CommandProperty, new Binding("Core.TestCommand"));
            Grid.SetRow(test, row); Grid.SetColumn(test, 3); grid.Children.Add(test);

            var on = PowerButton("ON", "Core.OnCommand", L("Allumer la prise", "Turn plug on"));
            on.CommandParameter = index;
            Grid.SetRow(on, row); Grid.SetColumn(on, 4); grid.Children.Add(on);

            var off = PowerButton("OFF", "Core.OffCommand", L("Éteindre la prise", "Turn plug off"));
            off.CommandParameter = index;
            Grid.SetRow(off, row); Grid.SetColumn(off, 5); grid.Children.Add(off);

            var state = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold };
            state.SetBinding(TextBlock.TextProperty, new Binding($"Core.Plug{index}State"));
            state.SetBinding(TextBlock.ForegroundProperty, new Binding($"Core.Plug{index}State") { Converter = new StateToBrushConverter() });
            Grid.SetRow(state, row); Grid.SetColumn(state, 6); grid.Children.Add(state);

            var protect = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
                ToolTip = L("Si coché, une confirmation est demandée avant d'éteindre cette prise",
                            "If checked, confirmation is required before turning off this plug")
            };
            protect.SetBinding(CheckBox.IsCheckedProperty, new Binding($"Core.Plug{index}ProtectOff") { Mode = BindingMode.TwoWay });
            Grid.SetRow(protect, row); Grid.SetColumn(protect, 7); grid.Children.Add(protect);

            var result = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            result.SetBinding(TextBlock.TextProperty, new Binding($"Core.Plug{index}Result"));
            result.SetBinding(TextBlock.ForegroundProperty, new Binding($"Core.Plug{index}Result") { Converter = new ResultToBrushConverter() });
            Grid.SetRow(result, row); Grid.SetColumn(result, 8); grid.Children.Add(result);
        }

        private static Button PowerButton(string label, string commandName, string tooltip)
        {
            var button = new Button
            {
                Content = label, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                Background = label == "ON" ? ShellyBrushes.OnBackground : ShellyBrushes.OffBackground,
                BorderBrush = Brushes.Transparent, Margin = new Thickness(0, 2, 4, 2),
                Padding = new Thickness(6, 3, 6, 3), ToolTip = tooltip
            };
            button.SetBinding(Button.CommandProperty, new Binding(commandName));
            return button;
        }
    }

    public static class ShellyBrushes
    {
        public static readonly SolidColorBrush OnBackground = Make(0x2E, 0x7D, 0x32);
        public static readonly SolidColorBrush OffBackground = Make(0xB7, 0x1C, 0x1C);
        public static readonly SolidColorBrush StateOn = Make(0x66, 0xBB, 0x6A);
        public static readonly SolidColorBrush StateOff = Make(0xEF, 0x53, 0x50);
        public static readonly SolidColorBrush StateUnknown = Make(0xFF, 0xB7, 0x4D);
        public static readonly SolidColorBrush Neutral = Make(0x9E, 0x9E, 0x9E);
        public static readonly SolidColorBrush Ok = StateOn;
        public static readonly SolidColorBrush Err = StateOff;

        private static SolidColorBrush Make(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }

    public class StateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? "";
            if (text.Contains("allumée") || text.Contains("on")) return ShellyBrushes.StateOn;
            if (text.Contains("éteinte") || text.Contains("off")) return ShellyBrushes.StateOff;
            if (text.Contains("inconnu") || text.Contains("unknown")) return ShellyBrushes.StateUnknown;
            return ShellyBrushes.Neutral;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class ResultToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? "";
            if (text.StartsWith("✔", StringComparison.Ordinal)) return ShellyBrushes.Ok;
            if (text.StartsWith("✖", StringComparison.Ordinal)) return ShellyBrushes.Err;
            return ShellyBrushes.Neutral;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}