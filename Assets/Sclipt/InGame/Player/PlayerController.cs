using Core.InterFace;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Core.MasterData;
using TPSRoguelite.InGame.Enum;

namespace TPSRoguelite.InGame.Player { 

public class PlayerController : MonoBehaviour
　　{
　　    //移動速度
　　    private const float moveSpeed = 5.0f;
　　
        //回転速度
　　    private const float ROTATE_SPEED = 10f;

        //レーザーポインターの描画距離
        private const float LASER_MAX_DISTANCE = 50f;

        //攻撃距離(射撃範囲)
        private const float ATACK_RANGE = 50;

　　    //物理演算コンポーネント
　　    [SerializeField] private Rigidbody rigidbody;

        //銃口のトランスフォーム
        [SerializeField] private Transform weponOrigin;
        
        //レーザープリンターの描画コンポーネント
        [SerializeField] private LineRenderer laserLineRenderer;

        //武器のID(デフォルトは1)
        [SerializeField] private ulong weaponId = 1;

        //マズルフラッシュのエフェクト
        [SerializeField] private ParticleSystem muzzleFlash;

        //武器のデータ
        private WeaponDataRecord CurrentWeapon;

        //自動生成されたインプット
        private PlayerInputActions inputActions;

        private Vector2 moveInput;

        private Transform mainCameraTransform;

        //リロードしているか
        private bool isReloading;

        //射撃可能か
        private bool canShot = true;

        //射撃のキャンセルトークン
        private CancellationTokenSource fireCts;
　　
        //現在の弾数
        public int CurrentAmmo {  get; private set; }

　　    //外部(アニメーションとかUIとか)に現在の速度を伝えるために保存する
　　    public Vector3 CurrentVelocity { get; private set; }
　　
　　    private void Awake()
　　    {
            gameObject.SetActive(false);
　　    }

        public void SetUp()
        {
            CurrentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(weaponId);

            if (CurrentWeapon != null)// ゲーム開始時に、マガジンに弾をフル装填する
            {
                CurrentAmmo = CurrentWeapon.MaxAmmo;
            }
            else
            {
                Debug.LogError("WeaponDataがありません");
            }

            inputActions = new PlayerInputActions();
            inputActions.Player.Fire.started += OnFire; //押し続けていると呼ばれる
            inputActions.Player.Fire.canceled += OnFire;
            inputActions.Player.Reload.performed += OnReload;


            if (UnityEngine.Camera.main != null)
            {
                mainCameraTransform = UnityEngine.Camera.main.transform;
            }
            else
            {
                Debug.LogError("MainCameraが見つかりません");
            }

            gameObject.SetActive(true);

        }

        private void OnEnable()
　　    {
　　        inputActions?.Enable();
　　    }
　　    private void OnDisable()
　　    {
　　        inputActions?.Disable();
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
　　        if(rigidbody == null || mainCameraTransform == null)
　　        {
　　            return;
　　        }

            Vector3 cameraforward = mainCameraTransform.forward;
            cameraforward.y= 0;
            cameraforward.Normalize();

            if(cameraforward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraforward);
                rigidbody.rotation = Quaternion.Slerp(rigidbody.rotation, targetRotation, ROTATE_SPEED * Time.deltaTime);
            }
　　
　　        //入力がない場合はピタッと止めておく
　　        if(moveInput == Vector2.zero)
　　        {
　　            rigidbody.linearVelocity = new Vector3(0,rigidbody.linearVelocity.y,0);
　　            CurrentVelocity = Vector3.zero;
　　            return;
　　        }
　　
　　        //カメラ基準の計算に変更
            Vector3 cameraRight = mainCameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraforward * moveInput.y + cameraRight * moveInput.x).normalized;

            Vector3 targetVelocity = moveDirection * moveSpeed;
            rigidbody.linearVelocity = new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.z);

　　        //外部(アニメーションとかUIとか)に現在の速度を伝えるために保存する
　　        CurrentVelocity = rigidbody.linearVelocity;
　　    }

        private void OnFire(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                // クールダウン中やリロード中（撃てない状態）なら、連打されても完全に無視する！
                if (!canShot || isReloading || CurrentWeapon == null)
                {
                    return;
                }
                fireCts = new CancellationTokenSource();
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(fireCts.Token, this.GetCancellationTokenOnDestroy());

                switch ((FireType)CurrentWeapon.WeaponFireType)
                {
                    case Enum.FireType.SemAuto:
                        ShootSemAutoAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case Enum.FireType.Burst:
                        ShootBurstAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case Enum.FireType.FullAuto:
                        ShootFireFullAutoAsync(linkedCts.Token).Forget();
                        break;

                    default:
                        Debug.LogWarning($"割り当てられていない攻撃タイプがあります{CurrentWeapon.WeaponFireType}");
                        break;
                }
            }
            if(context.canceled)
            {
                fireCts?.Cancel();
                fireCts?.Dispose();
                fireCts= null;
            }
        }
        private async UniTaskVoid ShootSemAutoAsync(CancellationToken token)
        {

            if(CurrentAmmo <= 0)
            {
                ReloadAsync().Forget();
                return;
            }
            canShot = false;

            CurrentAmmo--;
            Debug.Log($"セミオートで撃った!弾数残り{CurrentAmmo}");
            Shoot();

            await UniTask.Delay(System.TimeSpan.FromSeconds(CurrentWeapon.FireRate), cancellationToken:token);

            canShot = true;
        }

        private async UniTaskVoid ShootBurstAsync(CancellationToken token)
        {
            canShot = false;

            for (int i = 0; i < 3; i++)
            {
                if(CurrentAmmo <= 0)
                {
                    ReloadAsync().Forget();
                    break;
                }
                CurrentAmmo--;
                Shoot();
                Debug.Log($"バースト!残弾数 : {CurrentAmmo}");

                await UniTask.Delay(TimeSpan.FromSeconds(CurrentWeapon.Fireinterval),cancellationToken:token);
            }
            await UniTask.Delay(TimeSpan.FromSeconds(CurrentWeapon.FireRate),cancellationToken:token);
            canShot = true;
        }

        private async UniTaskVoid ShootFireFullAutoAsync(CancellationToken token)
        {
            canShot=false;

            while (!token.IsCancellationRequested)
            {
                if(CurrentAmmo <= 0)
                {
                    ReloadAsync().Forget();
                    break;
                }

                CurrentAmmo--;
                Debug.Log($"フルオート!残弾数 : {CurrentAmmo}");
                Shoot();

                bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(CurrentWeapon.Fireinterval), cancellationToken: token).SuppressCancellationThrow();
                if( isCanceled)
                {
                    break;
                }
            }
            await UniTask.Delay(TimeSpan.FromSeconds(CurrentWeapon.FireRate), cancellationToken:this.GetCancellationTokenOnDestroy());
            canShot = true;
        }
        //共通の攻撃処理
        private void Shoot()
        {
            if(muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);

            //光線に何かが当たったか判定
            if (Physics.Raycast(ray, out RaycastHit hitInfo, ATACK_RANGE))
            {
                Debug.Log($"{hitInfo.collider.name}に命中!");

                //当たった相手がIDamageableを持っているか
                IDamageable target = hitInfo.collider.GetComponent<IDamageable>();

                //
                if (target != null)
                {
                    target.TakeDamage(CurrentWeapon.AttackPower);
                }
            }

        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if (isReloading || CurrentAmmo == CurrentWeapon.MaxAmmo)
            {
                return;
            }
            ReloadAsync().Forget();
        }

        private async UniTask ReloadAsync()
        {
            isReloading = true;
            Debug.Log("リロード中");

            await UniTask.Delay(TimeSpan.FromSeconds(CurrentWeapon.ReloadTime), cancellationToken: this.GetCancellationTokenOnDestroy());

            CurrentAmmo = CurrentWeapon.MaxAmmo;
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
