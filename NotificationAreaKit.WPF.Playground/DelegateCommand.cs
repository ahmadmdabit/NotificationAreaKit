using System.Windows.Input;

namespace NotificationAreaKit.WPF.Playground;

// Simple ICommand implementation for MVVM-style actions
public class DelegateCommand : ICommand
{
    private readonly Action _execute;

    public event EventHandler? CanExecuteChanged;

    public DelegateCommand(Action execute) => _execute = execute;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}