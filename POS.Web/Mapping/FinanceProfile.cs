using AutoMapper;
using POS.DTOs.Finance;
using POS.Entities.Finance;

namespace POS.Web.Mapping
{
    public class FinanceProfile : Profile
    {
        public FinanceProfile()
        {
            CreateMap<POS.Entities.Common.CashTransactionType, POS.DTOs.Common.CashTransactionType>();

            CreateMap<ExpenseCategory, ExpenseCategoryDto>();
            CreateMap<CreateExpenseCategoryDto, ExpenseCategory>();
            CreateMap<UpdateExpenseCategoryDto, ExpenseCategory>();

            CreateMap<Expense, ExpenseDto>()
                .ForMember(d => d.ExpenseCategoryName, o => o.MapFrom(s => s.ExpenseCategory.Name))
                .ForMember(d => d.PaymentMethodName, o => o.MapFrom(s => s.PaymentMethod.Name));

            CreateMap<CreateExpenseDto, Expense>();

            CreateMap<CashTransaction, CashTransactionDto>()
                .ForMember(d => d.PaymentMethodName, o => o.MapFrom(s => s.PaymentMethod.Name));
            // No Create map — CashTransaction rows are only ever produced
            // internally whenever a Sale/Expense/SupplierPayment/etc. moves cash.

            CreateMap<DailyCashClosing, DailyCashClosingDto>();
            // OpeningBalance/TotalCashIn/TotalCashOut/ExpectedCash are computed
            // server-side from the day's CashTransactions — CreateDailyCashClosingDto
            // only carries what the till operator actually counted (ActualCash),
            // so build the entity manually in the service rather than mapping it.
        }
    }
}
