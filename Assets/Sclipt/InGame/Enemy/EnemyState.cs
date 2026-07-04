using UnityEngine;
using Core.InterFace;
using UnityEngine.Events;
using Core.MasterData;

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyState : MonoBehaviour,IDamageable
    {
        //敵のデータ
         public EnemyDataRecords EnemyDataAsset { get;private set; }

        //現在の体力
        public int CurrentHP { get; private set; }


        public event UnityAction<EnemyState> OnRetuenToPoolAction;
        
        private void OnEnable()
        {
            if (EnemyDataAsset == null)
            {
                Debug.LogError("EnemyDataがセットされていません");
                return;
            }
            CurrentHP = EnemyDataAsset.MaxHP;

        }

        public void TakeDamage(int damageAmount)
        {
            //マイナスのダメージを防ぐ
            if (damageAmount <= 0)
            {
                return;
            }

            CurrentHP -= damageAmount;
            Debug.Log($"{EnemyDataAsset.EnemyName}に{damageAmount}のダメージ!残りHP:{CurrentHP}");

            if (CurrentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"{EnemyDataAsset.EnemyName}を倒しました");
            gameObject.SetActive(false);
            OnRetuenToPoolAction?.Invoke(this);
        }
    }
}
