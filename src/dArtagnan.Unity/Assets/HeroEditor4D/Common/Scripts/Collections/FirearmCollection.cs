using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.HeroEditor4D.Common.Scripts.Collections
{
    /// <summary>
    ///     Global object that automatically grabs all required images.
    /// </summary>
    [CreateAssetMenu(fileName = "FirearmCollection", menuName = "HeroEditor4D/FirearmCollection")]
    public class FirearmCollection : ScriptableObject
    {
        public static bool AutoInitialize = true;
        public static Dictionary<string, FirearmCollection> Instances = new();
        public string Id;
        public List<FirearmParams> FirearmParams;

        [RuntimeInitializeOnLoadMethod]
        public static void RuntimeInitializeOnLoad()
        {
            if (AutoInitialize) Initialize();
        }

        public static void Initialize()
        {
            Instances = Resources.LoadAll<FirearmCollection>("").ToDictionary(i => i.Id, i => i);
        }
    }

    [Serializable]
    public class FirearmParams
    {
        public string Name;
        public ParticleSystem FireMuzzlePrefab;
        public AudioClip ShotSound;
        public AudioClip ReloadSound;
    }
}