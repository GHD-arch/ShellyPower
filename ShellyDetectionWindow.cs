using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Fenêtre de détection des prises Shelly sur le réseau local.
    /// Lance un balayage HTTP du sous-réseau (ShellyDiscovery), liste les prises trouvées
    /// et permet d'attribuer chaque adresse IP à l'une des 4 prises configurées.
    /// </summary>
    public class ShellyDetectionWindow : Window
    {
        private readonly ShellyPanelCore _core;
        private readonly StackPanel _rows;
        private readonly TextBlock _status;
        private readonly Button _scanButton;
        private CancellationTokenSource _cts;

        public ShellyDetectionWindow(ShellyPanelCore core)
        {
            _core = core;

            Title = "Shelly Power — Détection des prises sur le réseau";
            Width = 560;
            MinHeight = 220;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid { Margin = new Thickness(14) };
            for (var i = 0; i < 4; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Ligne 0 : bouton Rechercher + statut
            var top = new StackPanel { Orientation = Orientation.Horizontal };
            _scanButton = new Button
            {
                Content = "Rechercher",
                Padding = new Thickness(12, 4, 12, 4)
            };
            _scanButton.Click += (s, e) => StartScan();
            top.Children.Add(_scanButton);

            _status = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            top.Children.Add(_status);
            Grid.SetRow(top, 0);
            grid.Children.Add(top);

            // Ligne 1 : aide
            var help = new TextBlock
            {
                Text = "Sélectionnez, pour chaque prise détectée, l'emplacement (Prise 1 à 4) où attribuer son adresse IP. Les noms déjà saisis sont conservés.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 8)
            };
            Grid.SetRow(help, 1);
            grid.Children.Add(help);

            // Ligne 2 : résultats
            _rows = new StackPanel();
            Grid.SetRow(_rows, 2);
            grid.Children.Add(_rows);

            // Ligne 3 : fermer
            var close = new Button { Content = "Fermer", Padding = new Thickness(12, 4, 12, 4), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            close.Click += (s, e) => Close();
            Grid.SetRow(close, 3);
            grid.Children.Add(close);

            Content = grid;
            Loaded += (s, e) => StartScan();
        }

        private async void StartScan()
        {
            _scanButton.IsEnabled = false;
            _rows.Children.Clear();
            _status.Text = "Recherche des prises Shelly sur le réseau local…";

            List<ShellyDiscoveredDevice> found;
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                found = await Task.Run(() => ShellyDiscovery.DiscoverAsync(token));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (IsLoaded)
                {
                    _scanButton.IsEnabled = true;
                    _status.Text = "Erreur pendant la recherche : " + ex.Message;
                }

                return;
            }

            if (!IsLoaded)
            {
                return; // fenêtre fermée pendant la recherche
            }

            _scanButton.IsEnabled = true;
            _status.Text = found.Count == 0
                ? "Aucune prise Shelly détectée sur le réseau local."
                : $"{found.Count} prise(s) détectée(s) :";

            foreach (var device in found)
            {
                _rows.Children.Add(BuildRow(device));
            }
        }

        private FrameworkElement BuildRow(ShellyDiscoveredDevice device)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };

            var label = new TextBlock
            {
                Text = device.Label,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 330,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            row.Children.Add(label);

            var combo = new ComboBox { Width = 170, VerticalAlignment = VerticalAlignment.Center };
            combo.Items.Add("— attribuer à…");
            for (var i = 0; i < ShellyOptions.PlugCount; i++)
            {
                var ip = _core.GetPlugIpAt(i);
                var occupied = string.IsNullOrWhiteSpace(ip) ? "" : " (configurée)";
                combo.Items.Add($"Prise {i + 1}{occupied}");
            }

            // Pré-sélectionne l'emplacement si cette IP est déjà attribuée.
            for (var i = 0; i < ShellyOptions.PlugCount; i++)
            {
                if (string.Equals(_core.GetPlugIpAt(i), device.Ip, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i + 1;
                    break;
                }
            }

            combo.SelectionChanged += (s, e) =>
            {
                var slot = combo.SelectedIndex - 1;
                if (slot < 0 || slot >= ShellyOptions.PlugCount)
                {
                    return;
                }

                _core.AssignDiscovered(slot, device.Ip);
                _status.Text = $"Prise {slot + 1} ← {device.Ip} (sauvegardé)";
            };

            row.Children.Add(combo);
            return row;
        }
    }
}