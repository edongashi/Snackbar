﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
    }
}
