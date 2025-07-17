using UnityEngine;

namespace Networking
{
    [CreateAssetMenu(fileName = "NetworkManagerConfig", menuName = "d'Artagnan/NetworkManagerConfig", order = 0)]
    public class NetworkManagerConfig : ScriptableObject
    {
        public string awsHost;
        public string customHost;
        public bool useCustomHost;
        public int port;
    }
}