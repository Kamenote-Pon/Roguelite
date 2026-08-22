namespace TPSRoguelite.InGame.Enums
{
    // スキルの種類（CSVでは 0, 1, 2... の数字で指定します）
    public enum SkillType
    {
        MoveSpeedUp = 0,    // 移動速度アップ
        AttackPowerUp = 1,  // 攻撃力アップ
        FireRateUp = 2,     // 連射速度アップ（間隔短縮）
        ReloadSpeedUp = 3,  // リロード速度アップ（時間短縮）
        MaxAmmoUp = 4       // 最大弾数アップ
    }
}