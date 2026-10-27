namespace Project_D.Upgrades
{
    public enum UpgradeType
    {
        Projectiles,   // Thêm đạn mỗi loạt (+1, +2...)
        FireRate,      // Tăng tốc độ bắn theo % (+20%, +30%...)
        Damage,        // Tăng sát thương đạn cộng thẳng (+5, +10...)
        MoveSpeed,     // Tăng tốc độ di chuyển theo % (+15%, +20%...)
        MaxHealth,     // Tăng máu tối đa (+25, +50...)
        PickupRadius   // Tăng bán kính nam châm hút (+1.5m, +2m...)
    }
}
