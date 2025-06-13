using System;
using System.Collections.Generic;

namespace InvisibleManXRay.Handlers;

public enum ProxyEnabledOrDisabledState
{
    ENABLED,
    DISABLED
}

public class CoreEnabledOrDisabledModeObserver : Handler
{
    private readonly List<Action<ProxyEnabledOrDisabledState>> listeners = new();

    public void Subscribe(Action<ProxyEnabledOrDisabledState> listener)
    {
        listeners.Add(listener);
    }

    public void Unsubscribe(Action<ProxyEnabledOrDisabledState> listener)
    {
        listeners.Remove(listener);
    }

    public void Notify(ProxyEnabledOrDisabledState state)
    {
        foreach (var listener in listeners)
        {
            listener(state);
        }
    }
}