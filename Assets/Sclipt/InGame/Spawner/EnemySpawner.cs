using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TPSRoguelite.InGame.Enemy;
using Unity.VisualScripting;
using Core.MasterData;

namespace TPSRoguelite.InGame.Spawner
{
    public class EnemySpawner : MonoBehaviour
    {
        //出現時間
        private const float SPAWN_INTERVAL = 3.0f;

        //出現範囲
        private const float MAX_SPAWN_DISTANCE = 2.0f;

        //最初に用意する敵の数
        private const int POOL_SIZE = 20;

        //敵のプレハブ
        [SerializeField] GameObject enemyPrefab;

        //出現ポイント
        [SerializeField] private Transform[] spawnPoints;

        //敵を待機させておくプール
        private Queue<EnemyState> enemyPool = new Queue<EnemyState>();

        public void SetUp()
        {
            if (enemyPrefab == null)
            {
                return;
            }

            //ゲーム開始時に、あらかじめ用意した数だけ生成しておく
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject enemyobj = Instantiate(enemyPrefab);
                EnemyState enemy = enemyobj.GetComponent<EnemyState>();
                if (enemy != null)
                {
                    ulong randomId = (ulong)UnityEngine.Random.Range(1, MasterDataAccessor.Instance.Count<EnemyDataRecord>());
                    enemy.Initialize(randomId); 
                    enemy.gameObject.SetActive(false);
                    enemyPool.Enqueue(enemy);
                }
            }
            SpawnLoopAsync().Forget();

        }

        //UniTaskを用いた非同期の生成ループ
        private async UniTaskVoid SpawnLoopAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();

            //無限ループ
            while (true)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(SPAWN_INTERVAL));
                SpawneEnemyFromPool();
            }
        }

        //敵の生成
        private void SpawneEnemyFromPool()
        {
            if (enemyPrefab != null && spawnPoints.Length == 0)
            {
                return;
            }
            //ランダムな出現場所を決める
            int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];

            Vector3 safePosition = spawnPoint.position;
            if(NavMesh.SamplePosition(spawnPoint.position,out NavMeshHit hit,MAX_SPAWN_DISTANCE,NavMesh.AllAreas))
            {
                //見つかったら、安全な座標で上書きする
                safePosition = hit.position;
            }
            else
            {
                //見つからなかったら、生成をあきらめる
                Debug.LogWarning("近くに安全なスポーン位置が見つかりませんでした");
                return;
            }

            EnemyState enemy = null;

            if(enemyPool.Count > 0)
            {
                enemy = enemyPool.Dequeue();
            }
            else
            {
                Debug.LogWarning("プールに空きがなかったため、Instantiateで生成します。プールのサイズを増やすか、生成に時間をかけてください");
                GameObject enemyobj = Instantiate(enemyPrefab);
                enemy = enemyobj.GetComponent<EnemyState>();
                if(enemy == null )
                {
                    Debug.LogError("EnemyStateの取得に失敗しました");
                    return;
                }
            }
            enemy.OnRetuenToPoolAction -= RetuenToPool;
            enemy.OnRetuenToPoolAction += RetuenToPool;

            enemy.transform.position = safePosition;
            enemy.transform.rotation = spawnPoint.rotation;

            enemy.SetUp();
        }

        //プールへ戻す
        private void RetuenToPool(EnemyState enemy)
        {
            enemyPool.Enqueue(enemy);
            enemy.OnRetuenToPoolAction -= RetuenToPool;
        }
    }
}
