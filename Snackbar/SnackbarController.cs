using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Snackbar
{
    public class SnackbarController : IDisposable
    {
        private readonly object syncRoot;
        private bool isLooping;
        private readonly Queue<SnackbarMessage> messages;
        private readonly HashSet<Snackbar> snackbars;
        private SnackbarMessage currentMessage;
        private bool isOpen;

        public SnackbarController()
        {
            syncRoot = new object();
            messages = new Queue<SnackbarMessage>();
            snackbars = new HashSet<Snackbar>();
            ActionCommand = new RelayCommand(ActionClick);
        }

        public event EventHandler<SnackbarMessageEventArgs> MessageEnqueued;

        public event EventHandler<SnackbarMessageEventArgs> MessageDequeued;

        public event EventHandler<SnackbarMessageEventArgs> MessageCompleted;

        public ICommand ActionCommand { get; }

        public bool IsDisposed { get; private set; }

        public bool IsOpen
        {
            get { return isOpen; }
            private set
            {
                isOpen = value;
                foreach (var snackbar in snackbars)
                {
                    snackbar.Dispatcher.InvokeAsync(() => snackbar.IsOpen = value);
                }
            }
        }

        public SnackbarMessage CurrentMessage
        {
            get { return currentMessage; }
            private set
            {
                currentMessage = value;
                var content = value?.Content;
                var actionLabel = value?.ActionLabel;
                foreach (var snackbar in snackbars)
                {
                    snackbar.Dispatcher.InvokeAsync(() =>
                    {
                        snackbar.Content = content;
                        snackbar.ActionLabel = actionLabel;
                    });
                }
            }
        }

        public void AttachSnackbar(Snackbar snackbar)
        {
            if (snackbar == null)
            {
                return;
            }

            snackbars.Add(snackbar);
            var message = CurrentMessage;
            snackbar.Dispatcher.InvokeAsync(() =>
            {
                snackbar.Content = message?.Content;
                snackbar.ActionLabel = message?.ActionLabel;
                snackbar.ActionCommand = ActionCommand;
                snackbar.IsOpen = IsOpen;
            });
        }

        public void DetachSnackbar(Snackbar snackbar)
        {
            snackbars.Remove(snackbar);
        }

        public void Post(object content)
        {
            Post(new SnackbarMessage(content, null, null, false, SnackbarMessage.DefaultMessageDuration));
        }

        public void Post(object content, int duration)
        {
            Post(new SnackbarMessage(content, null, null, false, duration));
        }

        public void Post(object content, object actionLabel, Action<object> action)
        {
            Post(new SnackbarMessage(content, actionLabel, action, true, SnackbarMessage.DefaultMessageDuration));
        }

        public void Post(object content, object actionLabel, Action<object> action, int duration)
        {
            Post(new SnackbarMessage(content, actionLabel, action, true, duration));
        }

        public void Post(object content, object actionLabel, Action<object> action, bool closeOnAction)
        {
            Post(new SnackbarMessage(content, actionLabel, action, closeOnAction, SnackbarMessage.DefaultMessageDuration));
        }

        public void Post(object content, object actionLabel, Action<object> action, bool closeOnAction, int duration)
        {
            Post(new SnackbarMessage(content, actionLabel, action, closeOnAction, duration));
        }

        public List<SnackbarMessage> GetMessagesInQueue()
        {
            return messages.ToList();
        }

        private void Post(SnackbarMessage message)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            lock (syncRoot)
            {
                messages.Enqueue(message);
                message.State = SnackbarMessageState.Queued;
                if (!isLooping)
                {
                    isLooping = true;
                    Task.Run(Pump);
                }
            }

            MessageEnqueued?.Invoke(this, new SnackbarMessageEventArgs(message));
        }

        public void CloseCurrentMessage()
        {
            CurrentMessage?.CompleteTask(SnackbarMessageState.Removed);
        }

        public void ClearQueue()
        {
            lock (syncRoot)
            {
                foreach (var message in messages)
                {
                    message.State = SnackbarMessageState.Removed;
                }

                messages.Clear();
            }
        }

        private async Task Pump()
        {
            while (!IsDisposed)
            {
                SnackbarMessage message;
                lock (syncRoot)
                {
                    if (messages.Count == 0)
                    {
                        isLooping = false;
                        return;
                    }

                    message = messages.Dequeue();
                }

                CurrentMessage = message;
                var args = new SnackbarMessageEventArgs(message);
                MessageDequeued?.Invoke(this, args);
                message.State = SnackbarMessageState.FadingIn;
                IsOpen = true;
                await Task.WhenAny(message.DismissTask, Task.Delay(Snackbar.FadeInDuration));
                if (IsDisposed)
                {
                    CurrentMessage = null;
                    return;
                }

                var state = message.State;
                if (state == SnackbarMessageState.FadingIn)
                {
                    message.State = SnackbarMessageState.Visible;
                    await Task.WhenAny(message.DismissTask, Task.Delay(message.DisplayDuration));
                    if (IsDisposed)
                    {
                        CurrentMessage = null;
                        return;
                    }
                }

                message.State = SnackbarMessageState.FadingOut;
                IsOpen = false;
                await Task.Delay(Snackbar.FadeOutDuration);
                message.State = SnackbarMessageState.Completed;
                CurrentMessage = null;
                MessageCompleted?.Invoke(this, args);
            }
        }

        private void ActionClick(object parameter)
        {
            lock (syncRoot)
            {
                var message = CurrentMessage;
                if (message.State != SnackbarMessageState.Visible)
                {
                    return;
                }

                message.CompleteTask(SnackbarMessageState.ActionPerformed);
                message.Action?.Invoke(parameter);
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                lock (syncRoot)
                {
                    messages.Clear();
                }

                CurrentMessage?.CompleteTask(SnackbarMessageState.Removed);
                IsDisposed = true;
            }
        }
    }
}
