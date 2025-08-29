using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>ExternalServicerService</b> – Manages all external calibration service providers.
    /// Includes: CRUD for suppliers, integrations, contact management, and audit logging.
    /// </summary>
    public class ExternalServicerService
    {
        private readonly DatabaseService _db;

        public ExternalServicerService(DatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<List<Supplier>> GetAllAsync() => await _db.GetAllSuppliersAsync();

        /// <summary>Returns the supplier by ID (throws if not found to satisfy non-nullable result and avoid CS8603).</summary>
        public async Task<Supplier> GetByIdAsync(int id)
        {
            var s = await _db.GetSupplierByIdAsync(id);
            if (s == null) throw new KeyNotFoundException($"Supplier #{id} not found.");
            return s;
        }

        public async Task CreateAsync(Supplier supplier)
        {
            await _db.InsertOrUpdateSupplierAsync(supplier, false);
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            await _db.InsertOrUpdateSupplierAsync(supplier, true);
        }

        public async Task DeleteAsync(int supplierId)
        {
            await _db.DeleteSupplierAsync(supplierId);
        }
    }
}
