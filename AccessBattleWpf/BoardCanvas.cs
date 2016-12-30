using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AccessBattleWpf
{
    public class BoardCanvas : Canvas
    {
        // Board has a size of roughly 8x12 units
        // Width is 8
        // Height is 8 + Stack (2x1) + Empty space (2x1)

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            GuidelineSet guidelines = new GuidelineSet();
            guidelines.GuidelinesX.Add(1);
            guidelines.GuidelinesY.Add(1);
            dc.PushGuidelineSet(guidelines);            
            try
            {
                // Draw stuff here
                double width = ActualWidth;
                double height = ActualHeight;
                // Background
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, width, height));
                #region Area Check
                // Error if area too small ==================================
                if (height < 200 || width < 200)
                {
                    dc.DrawText(
                        new FormattedText(
                            "Area is too small!",
                            CultureInfo.InvariantCulture,
                            System.Windows.FlowDirection.LeftToRight,
                            new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                            10, Brushes.Red),
                        new Point(0, 0));
                    return;
                } // ========================================================
                #endregion

                double optimalHeight = width * 12 / 8;
                double optimalWidth = height * 8 / 12;
                var zero = new Point();
                double scale;
                if (optimalHeight > height)
                {
                    zero.X = (width - optimalWidth) / 2;
                    zero.Y = 0;
                    scale = 1;
                }
                else
                {
                    zero.X = 0;
                    zero.Y = (height - optimalHeight) / 2;
                    scale = 1;
                }

                DrawLink(dc, 0,0, scale);

            }
            catch { }
            finally
            {
                dc.Pop();
            }
        }        

        void DrawLink(DrawingContext dc, double x, double y, double scale)
        {

        }
    }
}
