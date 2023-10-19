using System.Text;

namespace PayArabic.DAO;

public class EventDao : BaseDao, IEventDao
{
    public IEnumerable<EventDTO> Get()
    {
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT TOP 10 Id, [Type], SendType, EntityType, EntityId, Sent, [Data], ScheduleDate
                            FROM [Event] 
                            WHERE ISNULL(Sent, 0) = 0   
                                AND InActive = 0 
                                AND ISNULL(SendType, '') != '' 
                                AND (ISNULL(ScheduleDate, 0) = 0 OR GETDATE() >= ScheduleDate)
                            ORDER BY Id");
        return DB.Query<EventDTO>(query.ToString()).ToList();
    }
    public void Update(long id, int isError = 0)
    {
        StringBuilder query = new StringBuilder();
        query.AppendLine("UPDATE [Event] SET Sent = 1, InActive = 1, IsError=" + isError + " WHERE Id = " + id);
        DB.Execute(query.ToString());
    }
}