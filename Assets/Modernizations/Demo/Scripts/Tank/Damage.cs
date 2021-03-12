using UnityEngine;
using System.Collections;
namespace Demo
{
	public class Damage : MonoBehaviour
	{
		public float AddDamage;
		public float ArmorMP;
		public float Importence;
		public Health MyHealth;
		// Use this for initialization
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			if (AddDamage > 0f)
			{
				MyHealth.Damage += AddDamage * Importence;
				AddDamage = 0f;
			}
		}
	}
}