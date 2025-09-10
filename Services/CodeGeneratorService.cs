using System;

namespace YasGMP.Services
{
    /// <summary>
    /// Simple code generator for creating unique machine identifiers.
    /// </summary>
    public class CodeGeneratorService
    {
        /// <summary>
        /// Generates a code in format M-yyyMMddHHmmssfff.
        /// </summary>
        public string GenerateMachineCode()
            => $"M-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }
}