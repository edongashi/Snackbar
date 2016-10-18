using System;
using System.Collections.ObjectModel;
using System.Windows;
using Snackbar;

namespace SnackbarDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            messageQueue = new ObservableCollection<SnackbarMessage>();
            AutoSnackbar.Controller.MessageEnqueued += (s1, e1) => messageQueue.Add(e1.SnackbarMessage);
            AutoSnackbar.Controller.MessageCompleted += (s1, e1) => Dispatcher.Invoke(() => messageQueue.RemoveAt(0));
            MessageQueueDataGrid.ItemsSource = messageQueue;
        }

        // Manual snackbar

        private int manualClickCount;

        private void Snackbar_ActionClick(object sender, RoutedEventArgs e)
        {
            CountTextBlock.Text = "Action clicked " + (++manualClickCount) + " times";
        }

        // Automatic snackbar
        private readonly ObservableCollection<SnackbarMessage> messageQueue;

        private int autoClickCount;
        private int postMessageNumber;

        private void PostMessage_Click(object sender, RoutedEventArgs e)
        {
            AutoSnackbar.Controller.Post(
                "(" + ++postMessageNumber + ") " + MessageTextBox.Text,
                NullIfEmpty(ActionTextBox.Text),
                parameter => AutoCountTextBlock.Text = "Action clicked " + (++autoClickCount) + " times");
        }

        private string NullIfEmpty(string value)
        {
            return value == string.Empty ? null : value;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            AutoSnackbar.Controller.IsFrozen = false;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            AutoSnackbar.Controller.IsFrozen = true;
        }
    }
}
