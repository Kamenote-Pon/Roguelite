using Core.InterFace;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Core.MasterData;
using TPSRoguelite.InGame.Enum;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using TPSRoguelite.InGame.Enums;
using TPSRoguelite.InGame.Manager;

namespace TPSRoguelite.InGame.Player
{

    public class PlayerController : MonoBehaviour
    {
        //移動速度
        private const float MOVE_SPEED = 5.0f;

        //回転速度
        private const float ROTATE_SPEED = 10f;

        //レーザーポインターの描画距離
        private const float LASER_MAX_DISTANCE = 50f;

        //攻撃距離(射撃範囲)
        private const float ATACK_RANGE = 50;

        private const float LEVEL_UP_EFFECT_DURATION = 2f;

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

        [Header("Weapon UI")]
        //武器の名前
        [SerializeField] private TextMeshProUGUI fireModeText;

        //弾のテキスト
        [SerializeField] private TextMeshProUGUI ammoText;

        [Header("Reload UI")]

        // リロード中のテキストと画像をまとめたオブジェクト
        [SerializeField] private GameObject reloadUI;

        // リロード中、「あと何秒で撃てるか」が分かるサークル画像
        [SerializeField] private Image reloadCircleImage;

        [Header("経験値＆レベルアップのUI")]

        // 経験値を表示するスライダーUI
        [SerializeField] private Slider expSlider;

        // レベルアップ時に表示するテキストUI
        [SerializeField] private TextMeshProUGUI levelUpText;

        // レベルアップ時のエフェクト
        [SerializeField] private ParticleSystem levelUpEffect;

        //武器のデータ
        private WeaponDataRecord currentWeapon;

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

        //スキルによるバフ
        private float moveSpeedBuff = 0f;
        private float attackPowerBuff = 0f;
        private float fireRateBuff = 0f;
        private float reloadSpeedBuff = 0f;
        private int maxAmmoBuff = 0;

        //現在の弾数
        public int CurrentAmmo { get; private set; }

        // 現在の経験値
        public int CurrentExp { get; private set; }

        // 現在のレベル
        public int CurrentLevel { get; private set; } = 1;

        private int RequiredExp => CurrentLevel * 5;

        private int FinalAttackPower => currentWeapon != null ? Mathf.RoundToInt(currentWeapon.AttackPower * (1f + attackPowerBuff)) : 0;

        private int FinalMaxAmmo => currentWeapon != null ? currentWeapon.MaxAmmo + maxAmmoBuff : 0;

        private float FinalReloadTime => currentWeapon != null ? currentWeapon.ReloadTime * Mathf.Max(0.1f, 1f - reloadSpeedBuff) : 0f;

        private float FinalFireRate => currentWeapon != null ? currentWeapon.FireRate * Mathf.Max(0.1f, 1f - fireRateBuff) : 0f;
        //外部(アニメーションとかUIとか)に現在の速度を伝えるために保存する
        public Vector3 CurrentVelocity { get; private set; }

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void SetUp()
        {
            currentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(weaponId);

            if (currentWeapon != null)// ゲーム開始時に、マガジンに弾をフル装填する
            {
                CurrentAmmo = currentWeapon.MaxAmmo;
                UpdateWeaponUI();
            }
            else
            {
                Debug.LogError("WeaponDataがありません");
            }
            CurrentExp = 0;
            CurrentLevel = 1;

            moveSpeedBuff = 0f;
            attackPowerBuff = 0f;
            reloadSpeedBuff = 0f;
            fireRateBuff = 0f;
            maxAmmoBuff = 0;

            if (levelUpText != null)
            {
                // レベルアップ時のテキストを非表示にする
                levelUpText.enabled = false;
            }

            UpdateExpUI();


            inputActions = new PlayerInputActions();
            inputActions.Player.Fire.performed += OnFire; //押し続けていると呼ばれる
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
            if (reloadUI != null)
            {
                reloadUI.SetActive(false);
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
            if (rigidbody == null || mainCameraTransform == null)
            {
                return;
            }

            Vector3 cameraforward = mainCameraTransform.forward;
            cameraforward.y = 0;
            cameraforward.Normalize();

            if (cameraforward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraforward);
                rigidbody.rotation = Quaternion.Slerp(rigidbody.rotation, targetRotation, ROTATE_SPEED * Time.deltaTime);
            }

            //入力がない場合はピタッと止めておく
            if (moveInput == Vector2.zero)
            {
                rigidbody.linearVelocity = new Vector3(0, rigidbody.linearVelocity.y, 0);
                CurrentVelocity = Vector3.zero;
                return;
            }

            //カメラ基準の計算に変更
            Vector3 cameraRight = mainCameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraforward * moveInput.y + cameraRight * moveInput.x).normalized;

            //物理演算で移動させる
            float FainalMoveSpeed = MOVE_SPEED* (1f + moveSpeedBuff);
            Vector3 targetVelocity = moveDirection * (FainalMoveSpeed);
            rigidbody.linearVelocity = new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.z);

            //外部(アニメーションとかUIとか)に現在の速度を伝えるために保存する
            CurrentVelocity = rigidbody.linearVelocity;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                // クールダウン中やリロード中（撃てない状態）なら、連打されても完全に無視する！
                if (!canShot || isReloading || currentWeapon == null)
                {
                    return;
                }
                fireCts = new CancellationTokenSource();
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(fireCts.Token, this.GetCancellationTokenOnDestroy());

                switch ((FireType)currentWeapon.WeaponFireType)
                {
                    case Enum.FireType.SemiAuto:
                        ShootSemAutoAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case Enum.FireType.Burst:
                        ShootBurstAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case Enum.FireType.FullAuto:
                        ShootFireFullAutoAsync(linkedCts.Token).Forget();
                        break;

                    default:
                        Debug.LogWarning($"割り当てられていない攻撃タイプがあります{currentWeapon.WeaponFireType}");
                        break;
                }
            }
            if (context.canceled)
            {
                fireCts?.Cancel();
                fireCts?.Dispose();
                fireCts = null;
            }
        }
        private async UniTaskVoid ShootSemAutoAsync(CancellationToken token)
        {

            if (CurrentAmmo <= 0)
            {
                Reload();
                return;
            }
            canShot = false;

            CurrentAmmo--;
            UpdateCurrentAmmoUI();
            Debug.Log($"セミオートで撃った!弾数残り{CurrentAmmo}");
            Shoot();

            await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: this.GetCancellationTokenOnDestroy());
            canShot = true;
        }

        private async UniTaskVoid ShootBurstAsync(CancellationToken token)
        {
            canShot = false;

            for (int i = 0; i < 3; i++)
            {
                if (CurrentAmmo <= 0)
                {
                    Reload();
                    break;
                }
                CurrentAmmo--;
                UpdateCurrentAmmoUI();
                Shoot();
                Debug.Log($"バースト!残弾数 : {CurrentAmmo}");

                await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.Fireinterval), cancellationToken: token);
            }
            await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: this.GetCancellationTokenOnDestroy());
            canShot = true;
        }

        private async UniTaskVoid ShootFireFullAutoAsync(CancellationToken token)
        {
            canShot = false;

            while (!token.IsCancellationRequested)
            {
                if (CurrentAmmo <= 0)
                {
                    Reload();
                    break;
                }

                CurrentAmmo--;
                UpdateCurrentAmmoUI();
                Debug.Log($"フルオート!残弾数 : {CurrentAmmo}");
                Shoot();

                bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.Fireinterval), cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    break;
                }
            }
            await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: this.GetCancellationTokenOnDestroy());
            canShot = true;
        }
        //共通の攻撃処理
        private void Shoot()
        {
            if (muzzleFlash != null)
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
                    int finalDamage = Mathf.RoundToInt(currentWeapon.AttackPower * (1f + attackPowerBuff));
                    target.TakeDamage(finalDamage);
                }
            }

        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if (isReloading || CurrentAmmo == FinalMaxAmmo)
            {
                return;
            }
            Reload();
        }

        private void Reload()
        {
            isReloading = true;
            if (reloadUI != null)
            {
                reloadUI.gameObject.SetActive(true);
            }

            if (reloadCircleImage != null)
            {
                reloadCircleImage.fillAmount = 0f;
            }

            float finalReloadTime = currentWeapon.ReloadTime * Mathf.Max(0.1f, 1f - reloadSpeedBuff);
            DOVirtual.Float(0f, 1f, FinalReloadTime, UpdateReloadUI).SetEase(Ease.Linear).OnComplete(FinishReload);
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
            if (Physics.Raycast(ray, out RaycastHit hitinfo, LASER_MAX_DISTANCE))
            {
                laserLineRenderer.SetPosition(1, hitinfo.point);
            }
            else
            {
                laserLineRenderer.SetPosition(1, ray.GetPoint(LASER_MAX_DISTANCE));
            }
        }
        // 武器タイプの表示を更新
        private void UpdateWeaponUI()
        {
            if (fireModeText == null || ammoText == null)
            {
                return;
            }

            FireType fireType = (FireType)currentWeapon.WeaponFireType;
            switch (fireType)
            {
                case FireType.SemiAuto:
                    fireModeText.text = "Semi-Auto";
                    fireModeText.color = Color.white;
                    break;
                case FireType.Burst:
                    fireModeText.text = "Burst";
                    fireModeText.color = Color.yellow;
                    break;
                case FireType.FullAuto:
                    fireModeText.text = "Full-Auto";
                    fireModeText.color = Color.red;
                    break;
                default:
                    fireModeText.text = "Unknown";
                    break;
            }

            UpdateCurrentAmmoUI();
        }

        // 弾薬表示の更新
        private void UpdateCurrentAmmoUI()
        {
            if (ammoText != null)
            {
                ammoText.text = $"{CurrentAmmo}/{FinalMaxAmmo}";
            }
        }
        // リロードUIの更新
        private void UpdateReloadUI(float value)
        {
            if (reloadCircleImage != null)
            {
                reloadCircleImage.fillAmount = value;
            }
        }

        // リロード終了処理
        private void FinishReload()
        {
            if (reloadUI != null)
            {
                reloadUI.SetActive(false);
            }

            CurrentAmmo = FinalMaxAmmo;
            UpdateCurrentAmmoUI();
            isReloading = false;
        }
        // 経験値を追加する
        public void AddExperience(int amount)
        {
            CurrentExp += amount;
            Debug.Log($"経験値を{amount}獲得！現在の経験値: {CurrentExp}");
            // レベルアップ判定
            if (CurrentExp >= RequiredExp)
            {
                LevelUp();
            }

            // UIゲージの長さを更新
            UpdateExpUI();
        }

        /// レベルアップ処理
        private void LevelUp()
        {
            // 余った経験値を消さずに、次のレベルに持ち越す
            CurrentExp -= RequiredExp;

            CurrentLevel++;

            Debug.Log($"レベルアップ！現在のレベル: {CurrentLevel}, 次のレベルまでの経験値: {RequiredExp - CurrentExp}");

            // レベルアップのエフェクトを再生
            if (levelUpEffect != null)
            {
                levelUpEffect.Play();
            }

            ShowLevelUpTextAsync().Forget();
        }

        // UIゲージの長さを更新する
        private void UpdateExpUI()
        {
            if (expSlider != null)
            {
                // 0.0（空） ～ 1.0（満タン） の割合を計算してSliderにセットする
                expSlider.value = (float)CurrentExp / RequiredExp;
            }
        }

        // レベルアップの文字を表示する非同期処理
        private async UniTaskVoid ShowLevelUpTextAsync()
        {
            if (levelUpText == null)
            {
                return;
            }

            levelUpText.enabled = true;
            levelUpText.SetText($"Level Up!\n<size=50%>Lv.{CurrentLevel}</size>");

            // 2秒間表示した後に非表示にする
            await UniTask.Delay(TimeSpan.FromSeconds(LEVEL_UP_EFFECT_DURATION), cancellationToken: this.GetCancellationTokenOnDestroy());

            levelUpText.enabled = false;

            LevelUpManager.Instance.OnLevelUp(inputActions, this);
        }
        public void ApplySkill(SkillDataRecord skill)
        {
            switch ((SkillType)skill.SkillType)
            {
                case SkillType.MoveSpeedUp:
                    moveSpeedBuff += skill.Value;
                    break;
                case SkillType.AttackPowerUp:
                    attackPowerBuff += skill.Value;
                    break;
                case SkillType.FireRateUp:
                    fireRateBuff += skill.Value;
                    break;
                case SkillType.ReloadSpeedUp:
                    reloadSpeedBuff += skill.Value;
                    break;
                case SkillType.MaxAmmoUp:
                    maxAmmoBuff += (int)skill.Value;
                    UpdateCurrentAmmoUI();
                    break;
            }
        }
    }
}