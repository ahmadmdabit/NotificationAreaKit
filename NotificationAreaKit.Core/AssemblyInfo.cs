using System.Runtime.CompilerServices;

// This attribute grants the NotificationAreaKit.WPF assembly access to the
// internal types and members of this NotificationAreaKit.Core assembly.
// This is the correct, secure way to allow our layered libraries to communicate
// without exposing internal implementation details to the final consumer.
[assembly: InternalsVisibleTo("NotificationAreaKit.WPF")]