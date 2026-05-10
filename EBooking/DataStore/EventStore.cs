namespace EBooking.DataStore;

using EBooking.Models;

public class EventStore
{
    private readonly List<Event> _events = [];

    public IReadOnlyCollection<Event> GetAll()
    {
        return _events.AsReadOnly();
    }

    public Event? GetById(Guid id)
    {
        return _events.FirstOrDefault(e => e.Id == id);
    }

    public void Add(Event eventItem)
    {
        _events.Add(eventItem);
    }

    public void Update(Event eventItem)
    {
        var index = _events.FindIndex(e => e.Id == eventItem.Id);

        if (index == -1)
        {
            return;
        }

        _events[index] = eventItem;
    }

    public void Delete(Guid id)
    {
        var eventItem = GetById(id);

        if (eventItem is null)
        {
            return;
        }

        _events.Remove(eventItem);
    }
}