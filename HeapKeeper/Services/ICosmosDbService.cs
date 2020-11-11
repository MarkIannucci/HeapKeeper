using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeapKeeper
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<CommentLinkUser>> GetItemsAsync(string query);
        Task<CommentLinkUser> GetItemAsync(string id);
        Task UpsertItem(CommentLinkUser commentLinkUser);
        Task DeleteItemAsync(string id);
    }
}