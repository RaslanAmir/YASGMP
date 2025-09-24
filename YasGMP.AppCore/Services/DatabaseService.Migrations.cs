using System;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Services
{
    /// <summary>
    /// Small, surgical, runtime migrations we can apply safely at app start.
    /// Specifically: reconcile machines triggers so "code" is never NULL.
    /// </summary>
    public sealed partial class DatabaseService
    {
        /// <summary>
        /// Drops conflicting BEFORE triggers on <c>machines</c> and creates a single
        /// canonical BEFORE INSERT trigger that guarantees non-null <c>code</c>.
        /// Safe to run multiple times.
        /// </summary>
        public async Task EnsureMachineTriggersForMachinesAsync(CancellationToken token = default)
        {
            try
            {
                // 1) Drop old/overlapping triggers if they exist
                var drops = new[]
                {
                    "DROP TRIGGER IF EXISTS `trg_m_sync`",
                    "DROP TRIGGER IF EXISTS `trg_machines_bi`",
                    "DROP TRIGGER IF EXISTS `bi_machines_code`",
                    "DROP TRIGGER IF EXISTS `trg_machines_bu_code`",
                    "DROP TRIGGER IF EXISTS `trg_machines_bi_all`"
                };
                foreach (var d in drops)
                    await ExecuteNonQueryAsync(d, null, token).ConfigureAwait(false);

                // 2) Create single BEFORE INSERT that sets internal_code/qr_payload and finally enforces code
                var createBefore = @"
CREATE TRIGGER `trg_machines_bi_all`
BEFORE INSERT ON `machines`
FOR EACH ROW
BEGIN
  DECLARE v_type_code VARCHAR(3);
  DECLARE v_mfr_code  VARCHAR(3);
  DECLARE v_next_id   BIGINT;

  -- name lookups are tolerant to NULL ids
  SELECT UPPER(LEFT(name,3)) INTO v_type_code FROM lkp_machine_types WHERE id = NEW.machine_type_id;
  IF v_type_code IS NULL OR v_type_code = '' THEN SET v_type_code = 'MCH'; END IF;

  SELECT UPPER(LEFT(name,3)) INTO v_mfr_code  FROM manufacturers     WHERE id = NEW.manufacturer_id;
  IF v_mfr_code  IS NULL OR v_mfr_code  = '' THEN SET v_mfr_code  = 'GEN'; END IF;

  SELECT AUTO_INCREMENT INTO v_next_id
    FROM information_schema.TABLES
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'machines';

  IF NEW.internal_code IS NULL OR NEW.internal_code = '' THEN
    SET NEW.internal_code = CONCAT(v_type_code,'-',v_mfr_code,'-',LPAD(COALESCE(v_next_id,1),5,'0'));
  END IF;

  IF NEW.qr_payload IS NULL OR NEW.qr_payload = '' THEN
    SET NEW.qr_payload = CONCAT('yasgmp://machine/', COALESCE(v_next_id,1));
  END IF;

  -- FINAL GUARD: ensure NEW.code is never NULL/empty
  IF NEW.code IS NULL OR CHAR_LENGTH(TRIM(NEW.code)) = 0 THEN
    SET NEW.code = CONCAT('MCH-', LPAD(COALESCE(v_next_id,1), 6, '0'));
  END IF;
END";
                await ExecuteNonQueryAsync(createBefore, null, token).ConfigureAwait(false);

                // 3) Keep code stable on UPDATE
                var createBeforeUpdate = @"
CREATE TRIGGER `trg_machines_bu_code`
BEFORE UPDATE ON `machines`
FOR EACH ROW
BEGIN
  IF NEW.code IS NULL OR NEW.code = '' THEN
    SET NEW.code = OLD.code;
  END IF;
END";
                await ExecuteNonQueryAsync(createBeforeUpdate, null, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Best-effort — surface a concise exception to the caller for diagnostics
                throw new InvalidOperationException("Failed to reconcile machines triggers: " + ex.Message, ex);
            }
        }
    }
}

