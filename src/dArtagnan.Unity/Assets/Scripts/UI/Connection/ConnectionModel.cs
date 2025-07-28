using R3;
using UnityEngine;

namespace UI.Connection
{
    [CreateAssetMenu(fileName = "ConnectionModel", menuName = "d'Artagnan/Connection Model", order = 0)]
    public class ConnectionModel : ScriptableObject
    {
        public SerializableReactiveProperty<string> ipEndpoint;
        public SerializableReactiveProperty<int> port;
        public SerializableReactiveProperty<string> awsEndpoint;
    }
}