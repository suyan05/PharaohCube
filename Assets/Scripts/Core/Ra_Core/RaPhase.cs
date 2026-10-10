// ¶óÀÇ ¹æ ³·/¹ã »óÅÂ
// F_RA_NIGHT_OPEN ÇÃ·¡±×°¡ ÄÑÁö¸é ¹ã. ÆÛÁñµéÀº ÀÌ°É º¸°í ³·/¹ã µ¥ÀÌÅÍ¸¦ °í¸§
public enum RaPhase
{
    Day,
    Night
}

public static class RaPhaseState
{
    public const string NightFlag = "F_RA_NIGHT_OPEN";

    public static bool IsNight => GameManager.Instance != null && GameManager.Instance.HasFlag(NightFlag);

    public static RaPhase Current => IsNight ? RaPhase.Night : RaPhase.Day;
}
