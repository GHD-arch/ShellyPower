using System;
using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Dictionnaire de ressources du plugin exporté via MEF — mécanisme officiel NINA
    /// (vérifié par réflexion IL de PluginLoader.Compose) : NINA fusionne chaque
    /// ResourceDictionary exporté par un plugin dans
    /// Application.Current.Resources.MergedDictionaries au chargement du plugin.
    ///
    /// Source pointe vers le XAML compilé (BAML) : le contenu est DIFFÉRÉ — matérialisé
    /// au premier accès sur le thread UI. C'est exactement le comportement des plugins
    /// officiels (TwoPointPolarAlignment…) et cela évite tous les problèmes d'affinité
    /// de thread rencontrés avec des DataTemplates créés en code (scellement au
    /// chargement / rendu cross-thread).
    /// </summary>
    [Export(typeof(ResourceDictionary))]
    public class PluginResources : ResourceDictionary
    {
        public PluginResources()
        {
            Source = new Uri("pack://application:,,,/NINA.ShellyPower;component/Resources/Templates.xaml");
        }
    }
}