using Microsoft.EntityFrameworkCore;
using POS.DTOs.Settings;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.IServices.Settings;
using POS.Entities.Settings;

namespace POS.Entities.Services
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly ApplicationDbContext _db;

        public PaymentMethodService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<PaymentMethodDto>> GetAllAsync()
        {
            var methods = await _db.PaymentMethods.OrderBy(m => m.Name).ToListAsync();
            return methods.Select(m => new PaymentMethodDto { Id = m.Id, Name = m.Name }).ToList();
        }

        public async Task<PaymentMethodDto?> GetByIdAsync(int id)
        {
            var m = await _db.PaymentMethods.FirstOrDefaultAsync(x => x.Id == id);
            return m is null ? null : new PaymentMethodDto { Id = m.Id, Name = m.Name };
        }

        public async Task<PaymentMethodDto> CreateAsync(CreatePaymentMethodDto dto)
        {
            var nameTaken = await _db.PaymentMethods.AnyAsync(m => m.Name.ToLower() == dto.Name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Payment method '{dto.Name}' already exists.");

            var method = new PaymentMethod { Name = dto.Name };
            _db.PaymentMethods.Add(method);
            await _db.SaveChangesAsync();

            return new PaymentMethodDto { Id = method.Id, Name = method.Name };
        }

        public async Task<PaymentMethodDto> UpdateAsync(UpdatePaymentMethodDto dto)
        {
            var method = await _db.PaymentMethods.FirstOrDefaultAsync(m => m.Id == dto.Id)
                ?? throw new KeyNotFoundException($"Payment method {dto.Id} not found.");

            var nameTaken = await _db.PaymentMethods.AnyAsync(m => m.Id != dto.Id && m.Name.ToLower() == dto.Name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"Payment method '{dto.Name}' already exists.");

            method.Name = dto.Name;
            await _db.SaveChangesAsync();

            return new PaymentMethodDto { Id = method.Id, Name = method.Name };
        }

        public async Task DeleteAsync(int id)
        {
            var method = await _db.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id)
                ?? throw new KeyNotFoundException($"Payment method {id} not found.");

            var inUse = await _db.Purchases.AnyAsync(p => p.PaymentMethodId == id);
            if (inUse)
                throw new InvalidOperationException("Cannot delete a payment method that is in use.");

            _db.PaymentMethods.Remove(method);
            await _db.SaveChangesAsync();
        }
    }
}