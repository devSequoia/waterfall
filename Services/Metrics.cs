using System.Diagnostics.Metrics;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable StringLiteralTypo

namespace waterfall.Services;

public abstract class Metrics
{
    private static Meter PgcrMeter { get; set; } = null!;

    public static Counter<int> PgcrDownloaded { get; private set; } = null!;

    public static void Initialize()
    {
        PgcrMeter = new Meter("PGCRScraper", "1.0.0");

        PgcrDownloaded = PgcrMeter.CreateCounter<int>(
            "pgcrs-downloaded",
            "PGCRs",
            "Number of PGCRs downloaded.");
    }
}