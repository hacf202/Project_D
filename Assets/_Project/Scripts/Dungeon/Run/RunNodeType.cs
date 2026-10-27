namespace Project_D.Dungeon.Run
{
    // Định nghĩa các loại phòng phân nhánh trong hành trình 13 tầng
    public enum RunNodeType
    {
        NormalBattle,   // Quái thường (3-4 waves)
        StarterReward,  // Nhận thưởng khởi đầu (Tầng 2)
        EliteBattle,    // Quái tinh anh nguy hiểm, thưởng cao
        MiniBoss,       // Mini Boss hội tụ (Tầng 7)
        BigReward,      // Buff thưởng lớn sau khi diệt Mini Boss (Tầng 8)
        RestSite,       // Lửa trại hồi máu (Miễn phí 30% hoặc Souls 100%)
        Shop,           // Cửa hàng mua thẻ / bình máu bằng Souls
        BuffReward,     // Điểm nhận thẻ buff lựa chọn
        BossBattle      // Trùm cuối kết thúc Run (Tầng 13)
    }
}
