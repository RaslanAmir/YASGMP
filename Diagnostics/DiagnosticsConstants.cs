using System;

namespace YasGMP.Diagnostics
{
    public static class DiagnosticsConstants
    {
        public const string EnvPrefix = "YAS_DIAG_";

        public const string KeyEnabled              = "Diagnostics:Enabled";
        public const string KeyLevel                = "Diagnostics:Level"; // trace|debug|info|warn|error|fatal
        public const string KeySinks                = "Diagnostics:Sinks"; // file,sqlite,stdout,elastic
        public const string KeySlowQueryMs          = "Diagnostics:SlowQueryMs";
        public const string KeyRedactionEnabled     = "Diagnostics:Redaction:Enabled";
        public const string KeySessionBreadcrumbs   = "Diagnostics:SessionBreadcrumbs";
        public const string KeyRollingMaxMb         = "Diagnostics:RollingFiles:MaxMB";
        public const string KeyRollingMaxDays       = "Diagnostics:RollingFiles:MaxDays";
        public const string KeyElasticUrl           = "Diagnostics:Elastic:Url";        // optional
        public const string KeyElasticIndex         = "Diagnostics:Elastic:Index";      // optional
        public const string KeyTopNRelease          = "Diagnostics:SqlSampling:TopNRelease"; // default 50
        public const string KeyRandomPercentRelease = "Diagnostics:SqlSampling:RandomPercentRelease"; // default 1

        public const int    DefaultSlowQueryMs      = 400;
        public const int    DefaultRollingMaxMb     = 32;
        public const int    DefaultRollingMaxDays   = 7;
        public const int    DefaultTopNRelease      = 50;
        public const double DefaultRandomPercentRel = 1.0;

        public const int    QueueCapacity           = 8192;
        public const int    QueueDrainBatch         = 256;
        public const int    QueueDrainIntervalMs    = 150;
    }

    public enum DiagLevel
    {
        Trace = 0,
        Debug = 1,
        Info  = 2,
        Warn  = 3,
        Error = 4,
        Fatal = 5
    }
}

