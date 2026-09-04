using System;
using System.Windows;
using System.Windows.Media;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Icônes vectorielles du plugin Shelly Power.
    /// Construites en code (pas en ressources) : PluginLoader écrase ExportMetadata("Icon")
    /// par Application.Current.Resources[key] — un indexeur qui NE traverse PAS les
    /// MergedDictionaries des plugins (d'où les icônes vides). En définissant Icon dans le
    /// constructeur et en OMETTANT la metadata Icon, la géométrie construite ici reste.
    /// Freeze() obligatoire : les classes sont instanciées par MEF sur un thread d'arrière-plan.
    /// </summary>
    public static class ShellyIcons
    {
        /// <summary>Icône « bouton power » (⏻) : anneau ouvert 270° + tige verticale.</summary>
        public static GeometryGroup BuildPowerIcon()
        {
            // Cercle : centre (8,8), rayon extérieur 6.5, rayon intérieur 4.5, gap 90° en haut.
            var cos45 = 0.7071;
            var ro = 6.5;
            var ri = 4.5;

            var ring = new PathGeometry();
            var fig = new PathFigure
            {
                StartPoint = new Point(8 - ro * cos45, 8 - ro * cos45),
                IsClosed = true,
            };
            fig.Segments.Add(new ArcSegment(
                new Point(8 + ro * cos45, 8 - ro * cos45),
                new Size(ro, ro), 0, true,
                SweepDirection.Clockwise, true));
            fig.Segments.Add(new LineSegment(
                new Point(8 + ri * cos45, 8 - ri * cos45), true));
            fig.Segments.Add(new ArcSegment(
                new Point(8 - ri * cos45, 8 - ri * cos45),
                new Size(ri, ri), 0, true,
                SweepDirection.Counterclockwise, true));
            ring.Figures.Add(fig);

            var stem = new RectangleGeometry(new Rect(7.25, 1, 1.5, 6));

            var group = new GeometryGroup();
            group.Children.Add(ring);
            group.Children.Add(stem);
            group.Freeze();
            return group;
        }
    }
}