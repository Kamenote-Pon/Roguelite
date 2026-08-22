using Unity.Mathematics;
using UnityEngine;

namespace TPSRoguelite.InGame.Camera {

   public class CameraController : MonoBehaviour
   {
    
       //追従するターゲット
       [SerializeField] private Transform target;

        
        [Header("カメラの基本設定")]

        //カメラの感度
        [SerializeField] private float lookSesitivity = 0.2f;

        //縦の最小角度
        [SerializeField] private float minPitch = -10f;

        //縦の最大角度
        [SerializeField] private float maxPitch = 60f;

        //ズーム速度
        [SerializeField] private float zoomSpeed = 5.0f;


        [Header("カメラの視点")]

        //後ろに下がる距離
        [SerializeField] private float targetDistance = 3.0f;

        //高さ
        [SerializeField] private float targetHeightOffset = 1.2f;

        //右にずらす距離(右肩くらい)
        [SerializeField] private float targetShoulderOffset = 0.8f;

       //自動生成されたクラス
       private PlayerInputActions inputActions;
  
       //マウスの移動量
       private Vector2 lookInput = Vector2.zero;
  
       //横の回転角度(Y軸回転)
       private float currentYaw = 0f;
  
       //縦の回転角度(X軸回転)
       private float currentPitch = 20f;

        //現在のカメラの位置(滑らかに動かすために必要)
        private float currentDistance = 0f;
        private float currentHeightOffset = 0f;
        private float currentShoulderOffset = 0f;
  
        private void Awake()
        {
            inputActions = new PlayerInputActions();
            
            //マウスカーソルを画面中央にロックして非表示
            Cursor.lockState = CursorLockMode.Locked; //画面中央にロック
            Cursor.visible = false; //マウスカーソルを非表示

            // 最初は通常時の視点をセットしておく
            currentDistance = targetDistance;
            currentHeightOffset = targetHeightOffset;
            currentShoulderOffset = targetShoulderOffset;
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
            //マウスの移動量を取得
            lookInput = inputActions.Player.Look.ReadValue<Vector2>();
  
            //感度を掛けて現在の角度に足し引きする
            currentYaw += lookInput.x * lookSesitivity;
            currentPitch -= lookInput.y * lookSesitivity;

            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        private void LateUpdate()
        {
            //カメラの移動は、プレイヤーの移動が終わった後に行う

            //ターゲットが設定されていない場合はエラー回避
            if(target == null)
            {
                return;
            }

            //現在の数値を、目線の数値に向かった滑らかに変化させる(変化させる機能が MathF.Lerp)
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);
            currentHeightOffset = Mathf.Lerp(currentHeightOffset,targetHeightOffset, zoomSpeed * Time.deltaTime);
            currentShoulderOffset = Mathf.Lerp(currentShoulderOffset, targetShoulderOffset, zoomSpeed * Time.deltaTime);

            //カメラの回転を計算
            Quaternion rotate = Quaternion.Euler(currentPitch, currentYaw, 0f);

            //注視点の計算(カメラが見るところ)
            Vector3 basePosition = target.position + Vector3.up * currentHeightOffset;

            //肩越しの起点にするために、カメラにとっての右方向へずらす
            Vector3 shoulderPosition = basePosition + (rotate * Vector3.right *  currentShoulderOffset);
            
            //カメラにとっての後ろ方向へ距離をずらす
            Vector3 cameraPosition = shoulderPosition + (rotate * Vector3.forward * currentDistance);

            //最終的な位置と回転を適用
            transform.position = cameraPosition;
            transform.rotation = rotate;
        }
    }
}
