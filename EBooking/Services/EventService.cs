namespace EBooking.Services;

using EBooking.DataStore;
using EBooking.DTO;
using EBooking.Exceptions;
using EBooking.Interfaces;
using EBooking.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для работы с мероприятиями
/// </summary>
public class EventsService : IEventsService
{
    private readonly AppDbContext _context;

    public EventsService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Получить список мероприятий с фильтрацией и пагинацией
    /// </summary>
    public async Task<PaginatedResult<EventDto>> GetEventsAsync(GetEventsQueryDto query)
    {
        if (query.Page <= 0)
        {
            throw new ValidationException("Page must be greater than 0");
        }

        if (query.PageSize <= 0)
        {
            throw new ValidationException("PageSize must be greater than 0");
        }

        IQueryable<Event> filteredEvents = _context.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var title = query.Title.ToLower();

            filteredEvents = filteredEvents.Where(e =>
                e.Title.ToLower().Contains(title));
        }

        if (query.From.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.EndAt <= query.To.Value);
        }

        var totalCount = await filteredEvents.CountAsync();

        var items = await filteredEvents
            .OrderBy(e => e.StartAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartAt = e.StartAt,
                EndAt = e.EndAt,
                TotalSeats = e.TotalSeats,
                AvailableSeats = e.AvailableSeats
            })
            .ToListAsync();

        return new PaginatedResult<EventDto>
        {
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            Items = items
        };
    }

    /// <summary>
    /// Получить мероприятие по идентификатору
    /// </summary>
    public async Task<EventDto> GetEventByIdAsync(Guid id)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        return ToDto(eventItem);
    }

    /// <summary>
    /// Создать новое мероприятие
    /// </summary>
    public async Task<EventDto> CreateEventAsync(CreateEventDto eventData)
    {
        ValidateEventDates(eventData.StartAt, eventData.EndAt);

        if (!eventData.TotalSeats.HasValue)
        {
            throw new ValidationException("TotalSeats required");
        }

        var newEvent = Event.Create(
            eventData.Title,
            eventData.Description,
            eventData.StartAt,
            eventData.EndAt,
            eventData.TotalSeats.Value
        );

        await _context.Events.AddAsync(newEvent);
        await _context.SaveChangesAsync();

        return ToDto(newEvent);
    }

    /// <summary>
    /// Обновить существующее мероприятие
    /// </summary>
    public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto eventData)
    {
        ValidateEventDates(eventData.StartAt, eventData.EndAt);

        var eventToUpdate = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventToUpdate is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        eventToUpdate.Title = eventData.Title;
        eventToUpdate.Description = eventData.Description;
        eventToUpdate.StartAt = eventData.StartAt;
        eventToUpdate.EndAt = eventData.EndAt;

        await _context.SaveChangesAsync();

        return ToDto(eventToUpdate);
    }

    /// <summary>
    /// Удалить мероприятие по идентификатору
    /// </summary>
    public async Task DeleteEventAsync(Guid id)
    {
        var eventToRemove = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventToRemove is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        _context.Events.Remove(eventToRemove);
        await _context.SaveChangesAsync();
    }

    private static EventDto ToDto(Event eventItem)
    {
        return new EventDto
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            StartAt = eventItem.StartAt,
            EndAt = eventItem.EndAt,
            TotalSeats = eventItem.TotalSeats,
            AvailableSeats = eventItem.AvailableSeats
        };
    }

    private static void ValidateEventDates(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new ValidationException("EndAt must be later than StartAt");
        }
    }
}