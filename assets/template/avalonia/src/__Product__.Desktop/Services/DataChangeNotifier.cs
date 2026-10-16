using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace __Product__.Desktop.Services;

// Repositories call this from background threads, so it hops to the UI thread before messaging.
public sealed class DataChangeNotifier : IDataChangeNotifier
{
    public void Notify(DataChanged change) =>
        Dispatcher.UIThread.Post(() => WeakReferenceMessenger.Default.Send(change));
}
