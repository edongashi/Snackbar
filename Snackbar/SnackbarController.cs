using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Snackbar
{
    public class SnackbarController : INotifyPropertyChanged, IDisposable
    {
        private readonly object syncRoot;
        private readonly Queue<SnackbarMessage> messages;
        private readonly HashSet<Snackbar> snackbars;
        private readonly HashSet<object> freezeTokens;
        private readonly RelayCommand actionCommand;
        private readonly ManualResetEventSlim messageActionEvent;
        private readonly ManualResetEventSlim frozenChangedEvent;
        private readonly ManualResetEventSlim unFrozenEvent;

        private int delayAfterActionClose;
        private bool isOpen;
        private bool isLooping;
        private bool isFrozen;
        private SnackbarMessage currentMessage;

        public SnackbarController()
        {
            delayAfterActionClose = 200;
            syncRoot = new object();
            messages = new Queue<SnackbarMessage>();
            freezeTokens = new HashSet<object>();
            snackbars = new HashSet<Snackbar>();
            actionCommand = new RelayCommand(ActionClick);
            messageActionEvent = new ManualResetEventSlim(false);
            frozenChangedEvent = new ManualResetEventSlim(false);
            unFrozenEvent = new ManualResetEventSlim(true);
        }

        public event EventHandler<SnackbarMessageEventArgs> MessageEnqueued;

        public event EventHandler<SnackbarMessageEventArgs> MessageDequeued;

        public event EventHandler<SnackbarMessageEventArgs> MessageCompleted;

        public bool IsFrozen
        {
            get { return isFrozen; }
            private set
            {
                if (value == isFrozen)
                {
                    return;
                }

                if (IsDisposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                isFrozen = value;
                if (value)
                {
                    unFrozenEvent.Reset();
                }
                else
                {
                    unFrozenEvent.Set();
                }

                frozenChangedEvent.Set();
                OnPropertyChanged(nameof(IsFrozen));
            }
        }

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

                OnPropertyChanged(nameof(IsOpen));
            }
        }

        public int DelayAfterActionClose
        {
            get { return delayAfterActionClose; }
            set
            {
                if (value < 0)
                {
                    return;
                }

                delayAfterActionClose = value;
                OnPropertyChanged(nameof(DelayAfterActionClose));
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

                OnPropertyChanged(nameof(CurrentMessage));
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
                snackbar.ActionCommand = actionCommand;
                snackbar.IsOpen = IsOpen;
            });
        }

        public void DetachSnackbar(Snackbar snackbar)
        {
            freezeTokens.Remove(snackbar);
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

        public void AddFreezeToken(object token)
        {
            if (token != null)
            {
                freezeTokens.Add(token);
                IsFrozen = true;
            }
        }

        public void RemoveFreezeToken(object token)
        {
            freezeTokens.Remove(token);
            IsFrozen = freezeTokens.Count != 0;
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
            var message = CurrentMessage;
            if (message != null && message.State != SnackbarMessageState.FadingOut)
            {
                message.State = SnackbarMessageState.Removed;
                messageActionEvent.Set();
            }
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
                unFrozenEvent.Wait();
                if (IsDisposed)
                {
                    return;
                }

                SnackbarMessage message;
                lock (syncRoot)
                {
                    if (messages.Count == 0)
                    {
                        isLooping = false;
                        return;
                    }

                    message = messages.Dequeue();
                    frozenChangedEvent.Reset();
                    messageActionEvent.Reset();
                }

                CurrentMessage = message;
                var args = new SnackbarMessageEventArgs(message);
                MessageDequeued?.Invoke(this, args);
                message.State = SnackbarMessageState.FadingIn;
                IsOpen = true;
                WaitHandle.WaitAny(new[]{
                    messageActionEvent.WaitHandle
                }, Snackbar.FadeInDuration);

                if (IsDisposed)
                {
                    CurrentMessage = null;
                    return;
                }

                if (message.State == SnackbarMessageState.FadingIn)
                {
                    message.State = SnackbarMessageState.Visible;
                    while (true)
                    {
                        var eventSource = WaitHandle.WaitAny(new[]
                        {
                            frozenChangedEvent.WaitHandle,
                            messageActionEvent.WaitHandle
                        }, message.DisplayDuration);

                        if (IsDisposed)
                        {
                            CurrentMessage = null;
                            return;
                        }

                        if (message.State == SnackbarMessageState.Removed)
                        {
                            break;
                        }

                        if (message.State == SnackbarMessageState.ActionPerformed && message.CloseOnAction)
                        {
                            await Task.Delay(DelayAfterActionClose);
                            break;
                        }

                        if (eventSource == 0 || IsFrozen)
                        {
                            frozenChangedEvent.Reset();
                            continue;
                        }

                        break;
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
                if (message == null || message.State != SnackbarMessageState.Visible)
                {
                    return;
                }

                message.State = SnackbarMessageState.ActionPerformed;
                if (message.CloseOnAction)
                {
                    messageActionEvent.Set();
                }

                message.Action?.Invoke(parameter);
            }
        }

        ~SnackbarController()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                lock (syncRoot)
                {
                    messages.Clear();
                }

                var message = CurrentMessage;
                if (message != null)
                {
                    message.State = SnackbarMessageState.Removed;
                }

                messageActionEvent.Set();
                unFrozenEvent.Set();
                frozenChangedEvent.Set();
                IsDisposed = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
