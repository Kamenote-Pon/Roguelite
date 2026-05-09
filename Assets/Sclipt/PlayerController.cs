using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //移動速度
    private const float moveSpeed = 5.0f;

    //物理演算コンポーネント
    [SerializeField] private Rigidbody rigidbody;

    //移動方向のベクトル
    private Vector3 moveDireection = Vector3.zero;

    //外部(アニメーションとかUIとか)に現在の速度を伝えるために保存する
    public Vector3 CurrentVelocity { get; private set; }


    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        //入力値から移動方向のベクトルを作成する
        moveDireection = new Vector3(x, 0, z).normalized;
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
        if(moveDireection == Vector3.zero)
        {
            rigidbody.linearVelocity = new Vector3(0,rigidbody.linearVelocity.y,0);
            CurrentVelocity = Vector3.zero;
            return;
        }

        //実際の移動速度計算
        Vector3 targetVelocity = moveDireection * moveSpeed;

        rigidbody.linearVelocity = new Vector3(
            targetVelocity.x,
            rigidbody.linearVelocity.y,
            targetVelocity.z);

        CurrentVelocity = rigidbody.linearVelocity;
    }
}
