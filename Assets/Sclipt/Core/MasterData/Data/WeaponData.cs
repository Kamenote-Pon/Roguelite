using UnityEngine;
using System;
using System.Collections.Generic;

namespace Core.MasterData
{
    [Serializable]
    public class WeaponDataRecord : IMasterData
    {
        [field: SerializeField] public ulong Id { get; private set; }

        //武器の名前
        [field: SerializeField] public string WeaponName { get; private set; }

        //射撃タイプ
        [field: SerializeField] public int WeaponFireType { get; private set; }

        //攻撃力
        [field: SerializeField] public int AttackPower { get; private set; }

        //射撃のインターバル
        [field: SerializeField] public float Fireinterval { get; private set; }

        //次の攻撃が打てるまで
        [field: SerializeField] public float FireRate { get; private set; }

        //最大弾数
        [field: SerializeField] public int MaxAmmo { get; private set; }

        //リロード時間
        [field: SerializeField] public float ReloadTime { get; private set; }
    }
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Scriptable Objects/WeaponData")]
    public class WeaponData : ScriptableObject, IMasterDataContainer<WeaponDataRecord>
    {
        [field: SerializeField] public List<WeaponDataRecord> Records { get; private set; }
    }
}
