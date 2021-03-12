using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Modernizations;
namespace Demo
{
    public class TurelControl : MonoBehaviour, IModifiable
    {
        public bool IsPlayer;
        public float Turel;
        [Modifiable("Turret rotation speed")]
        public float RotationSpeed = 60f;
        public float RotationAccel = 1f;
        public float RotationVelocity;
        public bool Fire;
        public bool Shoot;
        public Transform Gunpoint;
        public Transform GEO;
        public Transform ShootTrace;
        public float GunpointUpValue;
        public float GunpointDownValue;

        [Modifiable("Maximal damage")]
        public float MaxDamage;
        [Modifiable("Minimal damage")]
        public float MinDamage;

        float FireTimer;
        public float FireDelay;
        [Modifiable("Reloading time")]
        public float ReloadTimer;

        public LayerMask FireLayer;
        public LayerMask SceneLayer;

        public float Return;

        public int RaysCount = 50;
        [HideInInspector]
        public Transform Tr;

        private void Start()
        {
            Tr = transform;
        }

        void Update()
        {
            if (IsPlayer)
            {
                Turel = 0;
                if (Input.GetKey(KeyCode.X))
                    Turel = 1;
                else
                    if (Input.GetKey(KeyCode.Z))
                    Turel = -1;
                Fire = Input.GetKey(KeyCode.Space);
            }
            if (Mathf.Abs(RotationVelocity) < Mathf.Abs(Turel))
                RotationVelocity = Mathf.MoveTowards(RotationVelocity, Turel, Time.deltaTime * RotationAccel);
            else
                RotationVelocity = Mathf.MoveTowards(RotationVelocity, 0, Time.deltaTime * 10);
            transform.Rotate(0f, RotationVelocity * RotationSpeed * Time.deltaTime, 0f);

            FireTimer = Mathf.MoveTowards(FireTimer, 0f, Time.deltaTime);

            if ((Fire || Shoot) && FireTimer == 0f)
            {
                if (!Shoot)
                {
                    FireTimer = -FireDelay;
                    Shoot = true;
                    GEO.GetComponent<ParticleSystem>().Play();
                }
                else
                {
                    Shoot = false;
                    FireTimer = ReloadTimer;
                    RaycastHit[] Hits;
                    float NextAngleUp = 0f;
                    float NextAngleDown = 0f;
                    float AngleUp = GunpointUpValue / RaysCount;
                    float AngleDown = GunpointDownValue / RaysCount;
                    bool up = false;
                    bool shoot = false;
                    List<Transform> Exeptions = new List<Transform>(0);
                    int i = 0;
                    for (i = 0; i <= RaysCount * 2; i++)
                    {
                        RaycastHit Check;
                        if (!Physics.Raycast(GEO.position, GEO.forward, out Check, Mathf.Infinity, SceneLayer))
                            Check.distance = 2000;

                        Hits = Physics.RaycastAll(GEO.position, GEO.forward, Check.distance, FireLayer);
                        if (Hits.Length > 0)
                        {
                            foreach (RaycastHit Hit in Hits)
                            {
                                if (Check.distance >= Hit.distance)
                                {
                                    bool Exp = false;
                                    foreach (Transform Exep in Exeptions)
                                        if (Exep == Hit.collider.transform.root)
                                            Exp = true;
                                    if (!Exp)
                                    {
                                        Transform Root = Hit.collider.transform.root;
                                        Exeptions.Add(Root);
                                        if (Hit.collider.attachedRigidbody)
                                            Hit.collider.attachedRigidbody.AddForceAtPosition(GEO.forward * Return, Hit.point);
                                        if (Hit.collider.GetComponent<Damage>())
                                            Hit.collider.GetComponent<Damage>().AddDamage = Random.Range(MinDamage, MaxDamage);
                                    }
                                }
                            }
                            GetComponentInParent<Rigidbody>().AddForceAtPosition(-GEO.forward * Return, GEO.position);
                            Instantiate(ShootTrace, GEO.position, GEO.rotation);
                            shoot = true;
                            break;
                        }
                        if (up)
                        {
                            Gunpoint.Rotate(NextAngleUp, 0f, 0f);
                            NextAngleDown = NextAngleUp + AngleDown;
                        }
                        else
                        {
                            Gunpoint.Rotate(-NextAngleDown, 0f, 0f);
                            NextAngleUp = NextAngleDown + AngleUp;
                        }
                        up = !up;
                    }
                    if (!shoot)
                    {
                        Gunpoint.localRotation = Quaternion.identity;
                        Instantiate(ShootTrace, GEO.position, GEO.rotation);
                        GetComponentInParent<Rigidbody>().AddForceAtPosition(-GEO.forward * Return, GEO.position);
                    }
                }
            }
            Gunpoint.localRotation = Quaternion.RotateTowards(Gunpoint.localRotation, Quaternion.identity, Time.deltaTime * 10);

        }

        public int GetGroup()
        {
            return 0;
        }
    }
}