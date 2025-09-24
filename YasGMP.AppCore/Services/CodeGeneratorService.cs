using System;

namespace YasGMP.Services
{
    /// <summary>
    /// Simple code generator for creating unique machine identifiers.
    /// </summary>
    public class CodeGeneratorService
    {
        /// <summary>
        /// Generates a machine code based on name and manufacturer.
        /// Format: ABC-XYZ-yyyyMMddHHmmss, where ABC/XYZ are up to 3 letters/digits
        /// from the machine name and manufacturer respectively (padded with 'X' if missing).
        /// </summary>
        public string GenerateMachineCode(string? name, string? manufacturer)
        {
            static string Part(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "XXX";
                s = s.ToUpperInvariant();

                var chars = new char[3];
                int idx = 0;
                foreach (char c in s)
                {
                    if (char.IsLetterOrDigit(c))
                    {
                        chars[idx++] = c;
                        if (idx == 3) break;
                    }
                }
                for (; idx < 3; idx++) chars[idx] = 'X';
                return new string(chars);
            }

            string p1 = Part(name);
            string p2 = Part(manufacturer);
            return $"{p1}-{p2}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// Legacy overload using timestamp only (no name/manufacturer parts).
        /// Format: XXX-XXX-yyyyMMddHHmmss.
        /// </summary>
        public string GenerateMachineCode() => GenerateMachineCode(null, null);
    }
}
