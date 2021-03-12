using UnityEngine;
using System.Collections;
namespace Demo
{
    public class Health : MonoBehaviour
    {
        public bool IsPlayer;
        public float HitPoints;
        public float Damage;
        public float MaxHitPoints;
        public Color HPBarColor;
        public GUIStyle HPBarStyle;
        public Transform DeadReplaysment;
        public Transform DamageText;
        public float DamageEffectForce;
        void Start()
        {
        }

        void Update()
        {
            if (Damage > 0f)
            {
                Damage = Mathf.Min(Damage, HitPoints);

                HitPoints -= Damage;
                DamageEffectForce += Damage;

                if (IsPlayer == false && DamageText)
                {
                    Transform DText = Instantiate(DamageText, transform.position + Vector3.up, Quaternion.identity) as Transform;
                    DText.GetComponent<TextMesh>().text = "" + Mathf.Ceil(Damage);
                    DText.localScale = Vector3.one * (1 + (Camera.main.transform.position - transform.position).magnitude / 10);
                    Color C = Color.black;
                    C.a = 1f;
                    C.g = Damage / 250f;
                    C.r = (250 - Damage) / 250f;
                    DText.GetComponent<TextMesh>().color = C;
                }

                Damage = 0f;

            }

            if (HitPoints <= 0f)
            {
                if (IsPlayer == false)
                {
                    Storage.AddGlobalResource("Money", 15);
                    Storage.AddGlobalResource("Experience", 30);
                }
                if (DeadReplaysment)
                {
                    Transform dead = Instantiate(DeadReplaysment, transform.position, transform.rotation) as Transform;
                    dead.GetComponent<Rigidbody>().velocity = GetComponent<Rigidbody>().velocity;
                    dead.GetComponent<Rigidbody>().angularVelocity = GetComponent<Rigidbody>().angularVelocity;
                    dead.Find("Turret").localRotation = transform.Find("Turret").localRotation;
                }
                Destroy(gameObject);
            }
        }

        private void OnGUI()
        {
            if (IsPlayer)
            {
                GUI.backgroundColor = Color.gray;
                GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height - 100, 200, 10), "", HPBarStyle);
                GUI.backgroundColor = HPBarColor;
                GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height - 100, 200 * HitPoints / MaxHitPoints, 10), "", HPBarStyle);
            }
        }
    }


}