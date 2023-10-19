using Microsoft.Extensions.Logging;

namespace PayArabic.Core.Interfaces;
public interface IEventDao
{
    IEnumerable<EventDTO> Get();
    void Update(long id, int isError);
}