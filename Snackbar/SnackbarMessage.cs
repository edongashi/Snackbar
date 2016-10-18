using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Snackbar
{
    public class SnackbarMessage : INotifyPropertyChanged
    {
        public const int DefaultMessageDuration = 3000;
        private readonly TaskCompletionSource<SnackbarMessageState> taskCompletionSource;
        private SnackbarMessageState state;

        public SnackbarMessage(
            object content,
            object actionLabel,
            Action<object> action,
            bool closeOnAction,
            int displayDuration)
        {
            Content = content;
            ActionLabel = actionLabel;
            Action = action;
            CloseOnAction = closeOnAction;
            DisplayDuration = displayDuration <= 0 ? DefaultMessageDuration : displayDuration;
            taskCompletionSource = new TaskCompletionSource<SnackbarMessageState>();
        }

        public object Content { get; }

        public object ActionLabel { get; }

        public Action<object> Action { get; }

        public bool CloseOnAction { get; }

        public int DisplayDuration { get; }

        public SnackbarMessageState State
        {
            get { return state; }
            internal set
            {
                state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        internal Task DismissTask => taskCompletionSource.Task;

        internal void CompleteTask(SnackbarMessageState state)
        {
            if (DismissTask.IsCompleted)
            {
                return;
            }

            if (state == SnackbarMessageState.ActionPerformed && !CloseOnAction)
            {
                return;
            }

            State = state;
            taskCompletionSource.SetResult(state);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
