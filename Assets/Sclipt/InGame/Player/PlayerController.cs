using Core.InterFsce;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;

namespace TPSRoguelite.InGame.Player { 

public class PlayerController : MonoBehaviour
　　{
　　    //移動速度
　　    private const float moveSpeed = 5.0f;
　　
        //回転速度
　　    private const float ROTATE_SPEED = 10f;

        //レーザーポインターの描画距離
        private const float LASER_MAX_DISTANCE = 50f;

        //相手に与えるダメージ量
        private const int ATACK_DAMAGE = 20;

        //攻撃距離(射撃範囲)
        private const float ATACK_RANGE = 50;

        //最大弾数
        private const int MAX_AMMO = 30;

        //リロード時間
        private const float RELOAD_TIME = 1.5f;

　　    //物理演算コンポーネント
　　    [SerializeField] private Rigidbody rigidbody;

        //銃口のトランスフォーム
        [SerializeField] private Transform weponOrigin;
        
        //レーザープリンターの描画コンポーネント
        [SerializeField] private LineRenderer laserLineRenderer;

        //自動生成されたインプット
        private PlayerInputActions inputActions;

        private Vector2 moveInput;

        private Transform mainCameraTransform;

        //リロードしているか
        private bool isReloading;
　　
        //現在の弾数
        public int CurrentAmmo {  get; private set; }

　　    //外部(アニメーションとかUIとか)に現在の速度を伝えるために保存する
　　    public Vector3 CurrentVelocity { get; private set; }
　　
　　    private void Awake()
　　    {
            CurrentAmmo = MAX_AMMO;

　　        inputActions = new PlayerInputActions();
　　        inputActions.Player.Fire.performed += OnFire;
            inputActions.Player.Reload.performed += OnReload;

　　        if (UnityEngine.Camera.main != null) 
　　        {
　　            mainCameraTransform = UnityEngine.Camera.main.transform;
　　        }
　　        else
　　        {
　　            Debug.LogError("MainCameraが見つかりません");
　　        }
　　    }
　　
　　    private void OnEnable()
　　    {
　　        inputActions.Enable();
　　    }
　　    private void OnDisable()
　　    {
　　        inputActions.Disable();
　　    }
　　
　　
　　    void Update()
　　    {
　　        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            DrawLaserPointer();
　　    }
　　    private void FixedUpdate()
　　    {
　　        Move();
　　    }
　　    private void Move()//移動処理
　　    {
　　        if(rigidbody == null)
　　        {
　　            Debug.LogError("Rigidbodyが設定されていません");
　　            return;
　　        }
　　
　　        //入力がない場合はピタッと止めておく
　　        if(moveInput == Vector2.zero)
　　        {
　　            rigidbody.linearVelocity = new Vector3(0,rigidbody.linearVelocity.y,0);
　　            CurrentVelocity = Vector3.zero;
　　            return;
　　        }
　　
　　        //カメラ基準の計算に変更
            Vector3 cameraFoward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

            cameraFoward.y = 0f;
            cameraRight.y = 0f;
            cameraFoward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraFoward * moveInput.y + cameraRight * moveInput.x).normalized;

            //キャラクターを進行方向へ滑らかに振り向かせる
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rigidbody.rotation = Quaternion.Slerp(rigidbody.rotation,targetRotation,ROTATE_SPEED * Time.deltaTime);

            Vector3 targetVelocity = moveDirection * moveSpeed;
            rigidbody.linearVelocity = new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.z);

　　        //外部(アニメーションとかUIとか)に現在の速度を伝えるために保存する
　　        CurrentVelocity = rigidbody.linearVelocity;
　　    }
　　
　　    private void OnFire(InputAction.CallbackContext context)
　　    {
　　        Ray ray = new Ray(mainCameraTransform.position,mainCameraTransform.forward);

            //光線に何かが当たったか判定
            if (Physics.Raycast(ray,out RaycastHit hitInfo, ATACK_RANGE))
            {
                Debug.Log($"{hitInfo.collider.name}に命中!");

                //当たった相手がIDamageableを持っているか
                IDamageable target = hitInfo.collider.GetComponent<IDamageable>();

                //
                if (target != null)
                {
                    target.TakeDamage(ATACK_DAMAGE);
                }
            }
　　    }

        private void OnReload(InputAction.CallbackContext context)
        {
            if (isReloading || CurrentAmmo == MAX_AMMO)
            {
                return;
            }
            ReloadAsync().Forget();
        }

        private async UniTask ReloadAsync()
        {
            isReloading = true;
            Debug.Log("リロード中");

            await UniTask.Delay(TimeSpan.FromSeconds(RELOAD_TIME), cancellationToken: this.GetCancellationTokenOnDestroy());

            CurrentAmmo = MAX_AMMO;
            isReloading = false;
            Debug.Log("リロード完了");
        }


        //レーザーポインターの描画
        private void DrawLaserPointer()
        {
            if (laserLineRenderer == null || weponOrigin == null || mainCameraTransform == null)
            {
                return;
            }

            laserLineRenderer.SetPosition(0, weponOrigin.position);

            Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);
            if(Physics.Raycast(ray,out RaycastHit hitinfo,LASER_MAX_DISTANCE))
            {
                laserLineRenderer.SetPosition(1,hitinfo.point);
            }
            else
            {
                laserLineRenderer.SetPosition(1,ray.GetPoint(LASER_MAX_DISTANCE));
            }
        }
　　}
}
