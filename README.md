#Snackbar

WPF Snackbar that supports manual and automatic mode.

###Manual Mode

```xaml
<Snackbar IsOpen="..." Content="..." ActionLabel="..." ActionCommand="..." ActionParameter="..." />
```

###Automatic Mode

When Mode="Automatic" Snackbar.Controller controls snackbar content and queues messages.
The default controller can be used or a custom one can be bound for MVVM. 

```xaml
<Snackbar Mode="Automatic" Controller="..." />
```
```c#
// Queue a message to the controller
snackbar.Controller.Post(message, actionLabel, actionCallback); // +overloads
```

###Additional Properties
* ClosesOnRightClick - dismisses snackbar by right clicking. Works in both manual and automatic mode.
* FreezesOnMouseOver - pauses controller in automatic mode when hovering over the snackbar.
