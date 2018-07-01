using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AccessBattle.Wpf.View
{
    public class CenteredAdorner : Adorner
    {
        Control _child;

        public CenteredAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            _parent = adornedElement as FrameworkElement;
        }

        double _verticalRatio = .5;
        public double VerticalRatio
        {
            get { return _verticalRatio; }
            set
            {
                _verticalRatio = value;
                if (_verticalRatio < 0) _verticalRatio = 0;
                if (_verticalRatio > 1) _verticalRatio = 1;
                InvalidateMeasure();
            }
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Workaround for Grids that are shown in a ViewBox with uniform stretch.
        /// ArrangeOverride gets the wrong size in that case. Probably a WPF bug.
        /// </summary>
        public FrameworkElement _parent;

        public Control Child
        {
            get { return _child; }
            set
            {
                if (_child != null)
                {
                    RemoveVisualChild(_child);
                }
                _child = value;
                if (_child != null)
                {
                    AddVisualChild(_child);
                }
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException();
            return _child;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return constraint;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Point p;
            if (_parent != null)
            {
                p = new Point(
                (_parent.ActualWidth - _child.DesiredSize.Width) / 2,
                (_parent.ActualHeight - _child.DesiredSize.Height) * _verticalRatio);
            }
            else
                p = new Point(
                    (finalSize.Width - _child.DesiredSize.Width) / 2,
                    (finalSize.Height - _child.DesiredSize.Height) * _verticalRatio);

            Debug.WriteLine((int)finalSize.Width + ";" + (int)finalSize.Height);

            _child.Arrange(new Rect(p, _child.DesiredSize));
            return finalSize;
        }

    }
}
