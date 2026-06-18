using UnityEngine;
using TPSRoguelite.InGame.Enum;
using UnityEditor;

namespace TPSRoguelite.InGame.Data
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        //武器の名前
        [field:SerializeField] public string WeaponName { get;private set; }

        //射撃タイプ
        [field:SerializeField] public FireType WeaponFireType { get; private set; }

        //攻撃力
        [field:SerializeField] public int AttackPower {  get; private set; }

        //射撃のインターバル
        [field:SerializeField] public float Fireinterval {  get; private set; }

        //次の攻撃が打てるまで
        [field:SerializeField] public float FireRate {  get; private set; }

        //最大弾数
        [field:SerializeField] public int MaxAmmo {  get; private set; }

        //リロード時間
        [field:SerializeField] public float ReloadTime {  get; private set; }

    }

}