using System;

/// <summary>
/// Relógio do jogo. O expediente vai das 8h às 22h; ao fim do dia, o jogo passa para o dia seguinte.
/// Por padrão, 1 segundo real = 2 minutos no jogo (um dia de trabalho dura cerca de 7 minutos).
/// </summary>
public sealed class GameClock
{
    public const int DayStartHour = 8;
    public const int DayEndHour = 22;

    public float GameMinutesPerSecond { get; set; } = 2f;

    public int Day { get; private set; } = 1;

    /// <summary>Minutos desde a meia-noite.</summary>
    public float Minutes { get; private set; } = DayStartHour * 60;

    public int Hour => (int)(Minutes / 60f) % 24;
    public int Minute => (int)Minutes % 60;
    public string TimeText => $"{Hour:00}:{Minute:00}";

    /// <summary>0 no início do expediente, 1 no fim.</summary>
    public float DayProgress => Math.Max(0f, Math.Min(1f, (Minutes - DayStartHour * 60f) / ((DayEndHour - DayStartHour) * 60f)));

    public event Action<int> OnDayEnded;

    public bool Paused { get; set; }

    public void Advance(float realSeconds)
    {
        if (Paused || realSeconds <= 0) return;
        Minutes += realSeconds * GameMinutesPerSecond;
        if (Minutes >= DayEndHour * 60) EndDay();
    }

    /// <summary>Encerra o expediente e começa o próximo dia às 8h.</summary>
    public void EndDay()
    {
        int ended = Day;
        Day++;
        Minutes = DayStartHour * 60;
        OnDayEnded?.Invoke(ended);
    }

    public void Restore(int day, float minutes)
    {
        Day = Math.Max(1, day);
        Minutes = Math.Max(DayStartHour * 60, Math.Min(DayEndHour * 60 - 1, minutes));
    }
}
