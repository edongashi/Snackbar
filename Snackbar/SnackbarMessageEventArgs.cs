using System;

namespace Snackbar
{
    public class SnackbarMessageEventArgs : EventArgs
    {
        public SnackbarMessageEventArgs(SnackbarMessage snackbarMessage)
        {
            SnackbarMessage = snackbarMessage;
        }

        public SnackbarMessage SnackbarMessage { get; }
    }
}
