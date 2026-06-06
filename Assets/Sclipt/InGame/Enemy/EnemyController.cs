using UnityEngine;
using UnityEngine.AI;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        //
        //private const string PLAYER_TAG_NAME;

        //NavMeshAgent
        [SerializeField] private NavMeshAgent navMeshAgent = null;

        //目的地となるPlayerのTransform
        private Transform targetPlayer = null;

        //

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
    }
}
