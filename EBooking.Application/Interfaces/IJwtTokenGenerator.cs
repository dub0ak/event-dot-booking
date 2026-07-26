using EBooking.Domain.Entities;

namespace EBooking.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}