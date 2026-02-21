using System.Collections.Generic;
using System.Threading.Tasks;
using DW5820e_SMS.Models;

namespace DW5820e_SMS.Services
{
    public interface IDbService
    {
        Task InitializeAsync();
        Task<int> SaveMessageAsync(SmsMessage message);
        Task<List<SmsMessage>> GetAllMessagesAsync();
    }
}
