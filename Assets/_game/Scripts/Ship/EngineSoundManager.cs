using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineSoundManager : MonoBehaviour
{
    public static Dictionary<GameObject, EngineSoundManager> Instances;

    public AudioSet DrivesTensionOnStay;
    public AudioSet DrivesTensionOnHighVelocities;
    public AudioSet SpaceEngineTension;
    public AudioSet Giroscope;

    [System.Serializable]
    public class AudioSet
    {
        public AudioSource Source;
        public AnimationCurve Volume;
        public float VolumeSmooth = 1;
        public AnimationCurve Pitch;
        public float PitchSmooth = 1;

        public void Set(float volume, float pitch)
        {
            try
            {
                Source.volume = Mathf.Lerp(Source.volume, Volume.Evaluate(volume), Time.fixedDeltaTime / VolumeSmooth);
                Source.pitch = Mathf.Lerp(Source.pitch, Pitch.Evaluate(pitch), Time.fixedDeltaTime / PitchSmooth);
            }catch(System.Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    public void Set(float DrivesVelocity, float Tension, float GiroscopeForce)
    {
        SpaceEngineTension.Set(Tension, DrivesVelocity);
        DrivesTensionOnHighVelocities.Set(DrivesVelocity * Tension, DrivesVelocity);
        DrivesTensionOnStay.Set(Tension / DrivesVelocity, DrivesVelocity);
        Giroscope.Set(GiroscopeForce, GiroscopeForce);
    }

    void Awake()
    {
        if (Instances == null)
            Instances = new Dictionary<GameObject, EngineSoundManager>();
        Instances.Add(gameObject, this);

    }

}
