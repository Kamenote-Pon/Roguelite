using UnityEngine;
using Core.InterFsce;
using UnityEngine.Events;

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyState : MonoBehaviour,IDamageable
    {
        //‘Ì—Í‚ÌÅ‘å’l
        private const int MAX_HP = 100;

        //Œ»İ‚Ì‘Ì—Í
        public int CurrentHP { get; private set; }


        public event UnityAction<EnemyState> OnRetuenToPoolAction;
        
        private void Awake()
        {
                CurrentHP = MAX_HP;
        }

        private void OnEnable()
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
            gameObject.SetActive(false);
            OnRetuenToPoolAction?.Invoke(this);
        }
    }
}
