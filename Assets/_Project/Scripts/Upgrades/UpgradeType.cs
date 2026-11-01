namespace Project_D.Upgrades
{
    public enum UpgradeType
    {
        Projectiles,          // Thêm đạn mỗi loạt (+1, +2...)
        FireRate,             // Tăng tốc độ bắn người chơi theo % (+20%, +30%...)
        Damage,               // Tăng sát thương đạn người chơi (+5, +10...)
        MoveSpeed,            // Tăng tốc chạy người chơi theo % (+15%, +20%...)
        MaxHealth,            // Tăng máu tối đa người chơi (+25, +50...)
        PickupRadius,         // Tăng bán kính nam châm hút (+1.5m, +2m...)
        SummonMinion,         // Triệu hồi lính đồng minh mới hoặc bổ sung lính để Merge
        HealthRegen,          // Hồi máu theo thời gian cho người chơi (+1, +2 HP/s)
        MinionDamage,         // Tăng sát thương toàn bộ lính đồng minh (+25%, +50%...)
        MinionAttackSpeed,    // Tăng tốc độ đánh cho lính đồng minh (+20%, +30%...)
        MinionMaxHealth,      // Tăng máu tối đa cho lính đồng minh (+30%, +50%...)
        MinionRespawnSpeed    // Giảm thời gian đếm ngược hồi sinh lính (-25%, -40%...)
    }
}
