using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AccessBattle.Wpf.View
{
    /// <summary>
    /// Interaction logic for BoardFieldView.xaml
    /// </summary>
    public partial class BoardFieldView : UserControl
    {
        public BoardFieldView()
        {
            InitializeComponent();
            MouseDown += (s, e) => { if (CaptureMouse()) _clickStarted = true; };
            MouseLeave += (s, e) =>
            {
                if (IsMouseCaptured)
                    ReleaseMouseCapture();
                _clickStarted = false;  };
            MouseUp += (s, e) =>
            {
                if (IsMouseCaptured)
                    ReleaseMouseCapture();
                if (_clickStarted)
                {
                    _clickStarted = false;
                    Clicked?.Invoke(this, EventArgs.Empty);
                    var cmd = Command;
                    if (cmd != null && cmd.CanExecute(CommandParameter))
                        cmd.Execute(CommandParameter);
                }
            };


            DependencyPropertyDescriptor
                .FromProperty(FieldBorderBrushProperty, typeof(BoardFieldView))
                .AddValueChanged(this, (s, e) =>
                {
                    UpdateCurrentFieldBorderBrush();
                });
            DependencyPropertyDescriptor
                .FromProperty(IsSelectedProperty, typeof(BoardFieldView))
                .AddValueChanged(this, (s, e) =>
                {
                    UpdateCurrentFieldBorderBrush();
                });

            UpdateCurrentFieldBorderBrush();
        }

        void UpdateCurrentFieldBorderBrush()
        {
            CurrentFieldBorderBrush = IsSelected ? Brushes.Lime : FieldBorderBrush;
        }

        public event EventHandler Clicked;

        bool _clickStarted;
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command), typeof(ICommand), typeof(BoardFieldView), new PropertyMetadata(null));

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(BoardFieldView), new PropertyMetadata(null));

        public SolidColorBrush FieldBackground
        {
            get { return (SolidColorBrush)GetValue(FieldBackgroundProperty); }
            set { SetValue(FieldBackgroundProperty, value); }
        }
        public static readonly DependencyProperty FieldBackgroundProperty = DependencyProperty.Register(
            nameof(FieldBackground), typeof(SolidColorBrush), typeof(BoardFieldView), new PropertyMetadata(Brushes.Transparent));

        public SolidColorBrush FieldBorderBrush
        {
            get { return (SolidColorBrush)GetValue(FieldBorderBrushProperty); }
            set { SetValue(FieldBorderBrushProperty, value); }
        }
        public static readonly DependencyProperty FieldBorderBrushProperty = DependencyProperty.Register(
            nameof(FieldBorderBrush), typeof(SolidColorBrush), typeof(BoardFieldView), new PropertyMetadata(Brushes.Transparent));

        public SolidColorBrush CurrentFieldBorderBrush
        {
            get { return (SolidColorBrush)GetValue(CurrentFieldBorderBrushProperty.DependencyProperty); }
            protected set { SetValue(CurrentFieldBorderBrushProperty, value); }
        }
        public static readonly DependencyPropertyKey CurrentFieldBorderBrushProperty = DependencyProperty.RegisterReadOnly(
            nameof(CurrentFieldBorderBrush),
            typeof(SolidColorBrush),
            typeof(BoardFieldView),
            new PropertyMetadata(Brushes.Transparent));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(BoardFieldView), new PropertyMetadata(false));

        public bool IsVisibleToOpponent
        {
            get { return (bool)GetValue(IsVisibleToOpponentProperty); }
            set { SetValue(IsVisibleToOpponentProperty, value); }
        }
        public static readonly DependencyProperty IsVisibleToOpponentProperty = DependencyProperty.Register(
            nameof(IsVisibleToOpponent), typeof(bool), typeof(BoardFieldView), new PropertyMetadata(false));

        public BoardFieldVisualState FieldVisualState
        {
            get { return (BoardFieldVisualState)GetValue(BoardFieldVisualStateProperty); }
            set { SetValue(BoardFieldVisualStateProperty, value); }
        }
        public static readonly DependencyProperty BoardFieldVisualStateProperty = DependencyProperty.Register(
            nameof(FieldVisualState), typeof(BoardFieldVisualState), typeof(BoardFieldView), new PropertyMetadata(BoardFieldVisualState.Empty));

        public BoardFieldCardVisualState FieldCardVisualState
        {
            get { return (BoardFieldCardVisualState)GetValue(BoardFieldCardVisualStateProperty); }
            set { SetValue(BoardFieldCardVisualStateProperty, value); }
        }
        public static readonly DependencyProperty BoardFieldCardVisualStateProperty = DependencyProperty.Register(
            nameof(FieldCardVisualState), typeof(BoardFieldCardVisualState), typeof(BoardFieldView), new PropertyMetadata(BoardFieldCardVisualState.Empty));

        public bool IsExitFlipped
        {
            get { return (bool)GetValue(IsExitFlippedProperty); }
            set { SetValue(IsExitFlippedProperty, value); }
        }
        public static readonly DependencyProperty IsExitFlippedProperty = DependencyProperty.Register(
            nameof(IsExitFlipped), typeof(bool), typeof(BoardFieldView), new PropertyMetadata(false));

        public bool IsCardFlipped
        {
            get { return (bool)GetValue(IsCardFlippedProperty); }
            set { SetValue(IsCardFlippedProperty, value); }
        }
        public static readonly DependencyProperty IsCardFlippedProperty = DependencyProperty.Register(
            nameof(IsCardFlipped), typeof(bool), typeof(BoardFieldView), new PropertyMetadata(false));

        public bool IsHighlighted
        {
            get { return (bool)GetValue(IsHighlightedProperty); }
            set { SetValue(IsHighlightedProperty, value); }
        }
        public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(
            nameof(IsHighlighted), typeof(bool), typeof(BoardFieldView), new PropertyMetadata(false));

    }
}
