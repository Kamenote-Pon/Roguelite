using UnityEngine;
using Core.InterFsce;

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyStatu : MonoBehaviour,IDamageable
    {
        //‘Ì—Í‚ÌÅ‘å’l
        private const int MAX_HP = 100;

        //Œ»İ‚Ì‘Ì—Í
        public int CurrentHP { get; private set; }

        
        private void Awake()
        {
                CurrentHP = MAX_HP;
        }

        public void TakeDamage(int damageAmount)
        {
            //
            if (damageAmount <= 0)
            {
                return;
            }

            CurrentHP -= damageAmount;
            Debug.Log($"“G‚É{damageAmount}‚Ìƒ_ƒ[ƒW!c‚èHP:{CurrentHP}");

            if (CurrentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("“G‚ğ“|‚µ‚Ü‚µ‚½");
            Destroy(gameObject);
        }
    }
}
