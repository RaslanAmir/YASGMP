using System;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the diagnostics constants value.
    /// </summary>
    public static class DiagnosticsConstants
    {
        /// <summary>
        /// Represents the env prefix value.
        /// </summary>
        public const string EnvPrefix = "YAS_DIAG_";
        /// <summary>
        /// Represents the key enabled value.
        /// </summary>

        public const string KeyEnabled              = "Diagnostics:Enabled";
        /// <summary>
        /// Represents the key level value.
        /// </summary>
        public const string KeyLevel                = "Diagnostics:Level"; // trace|debug|info|warn|error|fatal
        /// <summary>
        /// Represents the key sinks value.
        /// </summary>
        public const string KeySinks                = "Diagnostics:Sinks"; // file,sqlite,stdout,elastic
        /// <summary>
        /// Represents the key slow query ms value.
        /// </summary>
        public const string KeySlowQueryMs          = "Diagnostics:SlowQueryMs";
        /// <summary>
        /// Represents the key redaction enabled value.
        /// </summary>
        public const string KeyRedactionEnabled     = "Diagnostics:Redaction:Enabled";
        /// <summary>
        /// Represents the key session breadcrumbs value.
        /// </summary>
        public const string KeySessionBreadcrumbs   = "Diagnostics:SessionBreadcrumbs";
        /// <summary>
        /// Represents the key rolling max mb value.
        /// </summary>
        public const string KeyRollingMaxMb         = "Diagnostics:RollingFiles:MaxMB";
        /// <summary>
        /// Represents the key rolling max days value.
        /// </summary>
        public const string KeyRollingMaxDays       = "Diagnostics:RollingFiles:MaxDays";
        /// <summary>
        /// Represents the key elastic url value.
        /// </summary>
        public const string KeyElasticUrl           = "Diagnostics:Elastic:Url";        // optional
        /// <summary>
        /// Represents the key elastic index value.
        /// </summary>
        public const string KeyElasticIndex         = "Diagnostics:Elastic:Index";      // optional
        /// <summary>
        /// Represents the key top n release value.
        /// </summary>
        public const string KeyTopNRelease          = "Diagnostics:SqlSampling:TopNRelease"; // default 50
        /// <summary>
        /// Represents the key random percent release value.
        /// </summary>
        public const string KeyRandomPercentRelease = "Diagnostics:SqlSampling:RandomPercentRelease"; // default 1
        /// <summary>
        /// Represents the default slow query ms value.
        /// </summary>

        public const int    DefaultSlowQueryMs      = 400;
        /// <summary>
        /// Represents the default rolling max mb value.
        /// </summary>
        public const int    DefaultRollingMaxMb     = 32;
        /// <summary>
        /// Represents the default rolling max days value.
        /// </summary>
        public const int    DefaultRollingMaxDays   = 7;
        /// <summary>
        /// Represents the default top n release value.
        /// </summary>
        public const int    DefaultTopNRelease      = 50;
        /// <summary>
        /// Represents the default random percent rel value.
        /// </summary>
        public const double DefaultRandomPercentRel = 1.0;
        /// <summary>
        /// Represents the queue capacity value.
        /// </summary>

        public const int    QueueCapacity           = 8192;
        /// <summary>
        /// Represents the queue drain batch value.
        /// </summary>
        public const int    QueueDrainBatch         = 256;
        /// <summary>
        /// Represents the queue drain interval ms value.
        /// </summary>
        public const int    QueueDrainIntervalMs    = 150;
    }
    /// <summary>
    /// Specifies the Diag Level enumeration.
    /// </summary>

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

