using UnityEngine;
using Core.InterFace;
using UnityEngine.Events;
using Core.MasterData;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Rendering.Universal;

namespace TPSRoguelite.InGame.Enemy 
{
    public class EnemyState : MonoBehaviour,IDamageable
    {
        //点滅時間
        private const float FLASH_DURATION = 0.1f;

        // オーブのドロップ位置の高さのオフセット
        private const float ORB_DROP_HEIGHT_OFFSET = 0.5f;

        //キャラクターのレンダラー
        [SerializeField] private Renderer[] modeRenderers;

        [Header("ドロップアイテム")]

        // ドロップするオーブ
        [SerializeField] private GameObject experienceOrbPrefab;

        //キャラクターの元々の色
        private Color[] defaultColors;

        //点滅するアニメーションのキャンセルトークン
        private CancellationTokenSource flashCts;

        //敵のデータ
         public EnemyDataRecord EnemyDataAsset { get;private set; }

        //現在の体力
        public int CurrentHP { get; private set; }


        public event UnityAction<EnemyState> OnRetuenToPoolAction;

        public event UnityAction OnDamageAction;
        
        public void Initialize(ulong id)
        {
            EnemyDataAsset = MasterDataAccessor.Instance.GetById<EnemyDataRecord>(id);

            if(modeRenderers != null)
            {
                defaultColors = new Color[modeRenderers.Length];
                for(int i = 0; i < modeRenderers.Length; i++)
                {
                    if(modeRenderers[i] != null)
                    {
                        defaultColors[i] = modeRenderers[i].material.color;
                    }
                }
            }
        }

        public void SetUp()
        {
            if (EnemyDataAsset == null)
            {
                Debug.LogError("EnemyDataがセットされていません");
                return;
            }
            CurrentHP = EnemyDataAsset.MaxHP;
            gameObject.SetActive(true);
            ResetColor();
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

            if(CurrentHP > 0)
            {
                OnDamageAction?.Invoke();

                flashCts?.Cancel();
                flashCts?.Dispose();
                flashCts = null;

                flashCts = new CancellationTokenSource();
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(flashCts.Token, this.GetCancellationTokenOnDestroy());

                DamageFlashAsync(linkedCts.Token).Forget();
            }
            else
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"{EnemyDataAsset.EnemyName}を倒しました");

            // 経験値オーブをドロップする
            if (experienceOrbPrefab != null)
            {
                Vector3 spawnPosition = transform.position + Vector3.up * ORB_DROP_HEIGHT_OFFSET;
                Instantiate(experienceOrbPrefab, spawnPosition, Quaternion.identity);
            }

            gameObject.SetActive(false);
            OnRetuenToPoolAction?.Invoke(this);
        }


        private async UniTaskVoid DamageFlashAsync(CancellationToken token)
        {
            if (modeRenderers == null)
            {
                return;
            }
            foreach(var renderer in modeRenderers)
            {
                if(renderer != null)
                {
                    renderer.material.color = Color.red;
                }
            }

            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(FLASH_DURATION), cancellationToken : token).SuppressCancellationThrow();
            if (!isCanceled)
            {
                ResetColor();
            }
        }
        //色をリセット
        private void ResetColor()
        {
            if(modeRenderers == null || defaultColors == null)
            {
                return;
            }
            for(int i = 0;i < modeRenderers.Length; i++)
            {
                if(modeRenderers[i] != null)
                {
                    modeRenderers[i].material.color = defaultColors[i];
                }
            }
        }

    }
}
