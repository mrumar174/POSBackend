using AutoMapper;
using Microsoft.EntityFrameworkCore;
using POS.DTOs.Purchasing;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.IServices.Purchasing;
using POS.Entities.Purchasing;

namespace POS.Entities.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public SupplierService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<SupplierDto>> GetAllAsync()
        {
            var suppliers = await _db.Suppliers.OrderBy(s => s.Name).ToListAsync();
            return _mapper.Map<List<SupplierDto>>(suppliers);
        }

        public async Task<SupplierDto?> GetByIdAsync(int id)
        {
            var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
            return supplier is null ? null : _mapper.Map<SupplierDto>(supplier);
        }

        public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
        {
            var nameTaken = await _db.Suppliers.AnyAsync(s => s.Name.ToLower() == dto.Name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Supplier '{dto.Name}' already exists.");

            var supplier = new Supplier
            {
                Name = dto.Name,
                ContactPerson = dto.ContactPerson,
                ContactNo = dto.ContactNo,
                Address = dto.Address,
                Email = dto.Email
            };

            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();

            return _mapper.Map<SupplierDto>(supplier);
        }

        public async Task<SupplierDto> UpdateAsync(UpdateSupplierDto dto)
        {
            var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == dto.Id)
                ?? throw new KeyNotFoundException($"Supplier {dto.Id} not found.");

            var nameTaken = await _db.Suppliers.AnyAsync(s => s.Id != dto.Id && s.Name.ToLower() == dto.Name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Supplier '{dto.Name}' already exists.");

            supplier.Name = dto.Name;
            supplier.ContactPerson = dto.ContactPerson;
            supplier.ContactNo = dto.ContactNo;
            supplier.Address = dto.Address;
            supplier.Email = dto.Email;

            await _db.SaveChangesAsync();

            return _mapper.Map<SupplierDto>(supplier);
        }

        public async Task DeleteAsync(int id)
        {
            var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Supplier {id} not found.");

            var hasPurchases = await _db.Purchases.AnyAsync(p => p.SupplierId == id);
            var hasPayments = await _db.SupplierPayments.AnyAsync(p => p.SupplierId == id);
            if (hasPurchases || hasPayments)
                throw new InvalidOperationException("Cannot delete a supplier that has purchase or payment history.");

            _db.Suppliers.Remove(supplier);
            await _db.SaveChangesAsync();
        }
    }
}