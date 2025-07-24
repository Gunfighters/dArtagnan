using System;

namespace dArtagnan.Unity.UI
{
    public interface IConnectionView
    {
        string IpAddress { get; }
        event Action OnConnectButtonClick;
    }
} 