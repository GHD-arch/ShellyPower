using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NINA.Equipment.Interfaces;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Compteur de consommation d'une prise Shelly, exposé à NINA comme switch « info only »
    /// (non pilotable). Value = puissance instantanée en watts ; la Description détaillée
    /// (tension, courant, énergie cumulée) est mise à jour à chaque Poll.
    /// NINA l'affiche dans la section « Read-only switches » de l'onglet Équipement et
    /// l'interroge via le même polling que les switchs pilotables.
    /// Implémente INotifyPropertyChanged pour rafraîchir l'affichage.
    /// </summary>
    public class ShellyPowerMeter : ISwitch, INotifyPropertyChanged
    {
        private readonly ShellyClient _client;
        private readonly string _ip;
        private double _value;
        private string _description = "";

        public ShellyPowerMeter(short id, string name, string ip)
        {
            Id = id;
            Name = name;
            _ip = ip;
            _client = new ShellyClient();
        }

        public short Id { get; }

        public string Name { get; }

        /// <summary>Détails de mesure (tension, courant, énergie), mis à jour au polling.</summary>
        public string Description
        {
            get => _description;
            private set
            {
                if (_description == value)
                {
                    return;
                }

                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        /// <summary>Puissance instantanée en watts.</summary>
        public double Value
        {
            get => _value;
            private set
            {
                if (Math.Abs(_value - value) < 0.05)
                {
                    return;
                }

                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public double Maximum => 5000;
        public double Minimum => 0;
        public double StepSize => 1;

        public bool Poll()
        {
            if (string.IsNullOrWhiteSpace(_ip))
            {
                return false;
            }

            try
            {
                var reading = _client.GetStatusAsync(_ip).GetAwaiter().GetResult();
                if (!reading.Ok)
                {
                    return false;
                }

                Value = reading.PowerW;
                Description = $"{reading.Voltage:0.#} V · {reading.CurrentA:0.00} A · {reading.EnergyKwh:0.000} kWh";
                return true;
            }
            catch
            {
                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}