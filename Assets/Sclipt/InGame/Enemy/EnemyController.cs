using UnityEngine;
using UnityEngine.AI;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SocialPlatforms;
using System;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        //
        private const string PLAYER_TAG_NAME ="Player";

        private const float KNOCKBACK_FOECE = 2.0f;
        private const float KNOCKBACK_DURARION = 0.15f;

        //敵の本体
        [SerializeField] private EnemyState enemystate = null;

        //NavMeshAgent
        [SerializeField] private NavMeshAgent navMeshAgent = null;

        //目的地となるPlayerのTransform
        private Transform targetPlayer = null;

        //ノックバック動作のキャンセルトークン
        private CancellationTokenSource hitCts;

        private void Awake()
        {
            //シーンからPlayerというタグがついたオブジェクトを探す
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetPlayer = player.transform;
            }
            else
            {
                //Debug.LogError($"{PLAYER_TAG_NAME}というタグの付いたオブジェクトが見つかりませんでした。");
                Debug.LogError("Playerというタグの付いたオブジェクトが見つかりませんでした。");

            }

            if(navMeshAgent != null && enemystate != null && enemystate.EnemyDataAsset != null)
            {
                navMeshAgent.speed = enemystate.EnemyDataAsset.MoveSpeed;
            }

        }

        void Update()
        {
            //ターゲット(プレイヤー)とナビが存在しているか
            if (targetPlayer != null && navMeshAgent != null)
            {
                //プレイヤーの現在位置を毎フレーム目的地としてセット
                navMeshAgent.SetDestination(targetPlayer.position);
            }
        }

        private void OnEnable()
        {
            if (enemystate != null)
            {
                enemystate.OnDamageAction -= HandleDamage;
                enemystate.OnDamageAction += HandleDamage;
            }
        }

        private void OnDisable()
        {
            if(enemystate != null)
            {
                enemystate.OnDamageAction -= HandleDamage;
            }
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = false;
            }
        }

        private async UniTaskVoid KnockbackAsync(CancellationToken token)
        {
            if(navMeshAgent == null)
            {
                return;
            }
            bool wasStopped = navMeshAgent.isStopped;
            navMeshAgent.isStopped = true;

            if(targetPlayer != null)
            {
                Vector3 dir = (transform.position - targetPlayer.position).normalized;
                dir.y = 0;
                transform.position += dir * KNOCKBACK_FOECE;
            }
            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(KNOCKBACK_DURARION), cancellationToken: token).SuppressCancellationThrow();

            if (!isCanceled && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = wasStopped;
            }
        }
        private void HandleDamage()
        {
            hitCts?.Cancel();
            hitCts?.Dispose();
            hitCts = null;

            hitCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(hitCts.Token, this.GetCancellationTokenOnDestroy());

            KnockbackAsync(linkedCts.Token).Forget();
        }
    }
}
