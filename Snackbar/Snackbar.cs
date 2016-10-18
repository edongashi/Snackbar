using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Snackbar
{
    /// <summary>
    /// Implements a <see cref="Snackbar"/> inspired by the Material Design specs (https://material.google.com/components/snackbars-toasts.html).
    /// </summary>
    public class Snackbar : ContentControl
    {
        public const string PartActionButtonName = "PART_actionButton";
        public const int FadeInDuration = 300;
        public const int FadeOutDuration = 300;

        static Snackbar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Snackbar), new FrameworkPropertyMetadata(typeof(Snackbar)));
            TagProperty.OverrideMetadata(typeof(Snackbar), new FrameworkPropertyMetadata("0"));
        }

        public static readonly RoutedEvent ActionClickEvent = EventManager.RegisterRoutedEvent(
            nameof(ActionClick),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(Snackbar));

        public static readonly DependencyProperty ActionLabelProperty = DependencyProperty.Register(
            nameof(ActionLabel),
            typeof(object),
            typeof(Snackbar),
            new PropertyMetadata(null));

        public static readonly DependencyProperty ActionCommandProperty = DependencyProperty.Register(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(Snackbar),
            new PropertyMetadata(null));

        public static readonly DependencyProperty ActionCommandParameterProperty = DependencyProperty.Register(
            nameof(ActionCommandParameter),
            typeof(object),
            typeof(Snackbar),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(Snackbar),
            new PropertyMetadata(false));

        public static readonly DependencyProperty ClosesOnRightClickProperty = DependencyProperty.Register(
            nameof(ClosesOnRightClick),
            typeof(bool),
            typeof(Snackbar),
            new PropertyMetadata(false));

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode),
            typeof(SnackbarMode),
            typeof(Snackbar),
            new PropertyMetadata(SnackbarMode.Manual, ModeChangedCallback));

        public static readonly DependencyProperty ControllerProperty = DependencyProperty.Register(
            nameof(Controller),
            typeof(SnackbarController),
            typeof(Snackbar),
            new PropertyMetadata(null, ControllerChangedCallback));

        private static void ModeChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var snackbar = (Snackbar)dependencyObject;
            var newMode = (SnackbarMode)e.NewValue;
            if (newMode == SnackbarMode.Automatic)
            {
                snackbar.Controller?.AttachSnackbar(snackbar);
            }
            else
            {
                snackbar.Controller?.DetachSnackbar(snackbar);
            }
        }

        private static void ControllerChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var snackbar = (Snackbar)dependencyObject;
            var oldController = (SnackbarController)e.OldValue;
            oldController?.DetachSnackbar((Snackbar)dependencyObject);
            var newController = (SnackbarController)e.NewValue;
            if (snackbar.Mode == SnackbarMode.Automatic)
            {
                newController?.AttachSnackbar(snackbar);
            }
        }

        /// <summary>
        /// An event raised by clicking on the action button.
        /// </summary>
        public event RoutedEventHandler ActionClick
        {
            add { AddHandler(ActionClickEvent, value); }
            remove { RemoveHandler(ActionClickEvent, value); }
        }

        /// <summary>
        /// The label for the action button. A null value will completely hide the action button.
        /// </summary>
        public object ActionLabel
        {
            get { return GetValue(ActionLabelProperty); }
            set { SetValue(ActionLabelProperty, value); }
        }

        /// <summary>
        /// A command by clicking on the action button.
        /// </summary>
        public ICommand ActionCommand
        {
            get { return (ICommand)GetValue(ActionCommandProperty); }
            set { SetValue(ActionCommandProperty, value); }
        }

        /// <summary>
        /// A parameter for the <see cref="ActionCommand"/>.
        /// </summary>
        public object ActionCommandParameter
        {
            get { return GetValue(ActionCommandParameterProperty); }
            set { SetValue(ActionCommandParameterProperty, value); }
        }

        /// <summary>
        /// Returns true if the <see cref="Snackbar"/> is visible or false otherwise. Setting this property shows or hides the <see cref="Snackbar"/>.
        /// </summary>
        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether right clicking the <see cref="Snackbar"/> will close it.
        /// </summary>
        public bool ClosesOnRightClick
        {
            get { return (bool)GetValue(ClosesOnRightClickProperty); }
            set { SetValue(ClosesOnRightClickProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public SnackbarMode Mode
        {
            get { return (SnackbarMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public SnackbarController Controller
        {
            get { return (SnackbarController)GetValue(ActionCommandParameterProperty); }
            set { SetValue(ActionCommandParameterProperty, value); }
        }

        private Button actionButton;

        public Snackbar()
        {
            Controller = new SnackbarController();
            MouseRightButtonDown += OnMouseRightButtonDown;
            Loaded += SnackbarLoaded;
            Unloaded += SnackbarUnloaded;
        }

        private void SnackbarLoaded(object sender, RoutedEventArgs e)
        {
            if (Mode == SnackbarMode.Automatic)
            {
                Controller?.AttachSnackbar(this);
            }
        }

        private void SnackbarUnloaded(object sender, RoutedEventArgs e)
        {
            Controller?.DetachSnackbar(this);
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!ClosesOnRightClick)
            {
                return;
            }

            if (Mode == SnackbarMode.Manual)
            {
                SetCurrentValue(IsOpenProperty, false);
            }
            else
            {
                Controller?.CloseCurrentMessage();
            }

            e.Handled = true;
        }

        public override void OnApplyTemplate()
        {
            if (actionButton != null)
            {
                actionButton.Click -= ActionButtonClickHandler;
            }

            actionButton = GetTemplateChild(PartActionButtonName) as Button;

            if (actionButton != null)
            {
                actionButton.Click += ActionButtonClickHandler;
            }

            base.OnApplyTemplate();
        }

        private void ActionButtonClickHandler(object sender, RoutedEventArgs args)
        {
            // raise the event and call the command
            RoutedEventArgs routedEventArgs = new RoutedEventArgs(ActionClickEvent, this);
            RaiseEvent(routedEventArgs);

            if (ActionCommand != null && ActionCommand.CanExecute(ActionCommandParameter))
            {
                ActionCommand.Execute(ActionCommandParameter);
            }

            args.Handled = true;
        }
    }
}
