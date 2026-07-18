using Cysharp.Threading.Tasks;
using TPSRoguelite.InGame.Player;
using TPSRoguelite.InGame.Spawner;
using UnityEngine;
using Core.MasterData;

namespace TPSRoguelite.InGame.Manager
{

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance {  get; private set; }

        [SerializeField] private PlayerController player = null;
        [SerializeField] private EnemySpawner enemySpawner = null;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        void Start()
        {
            SetUp().Forget();
        }
        private async UniTaskVoid SetUp()
        {
            //マスターデータの読み込み
            await MasterDataAccessor.Instance.InitializeAsync();

            //読み込みが完了したら、プレイヤーとスポナーの準備を始める
            if(player != null)
            {
                player.SetUp();
            }
            if(enemySpawner != null)
            {
                enemySpawner.SetUp();
            }
        }
    }
}
