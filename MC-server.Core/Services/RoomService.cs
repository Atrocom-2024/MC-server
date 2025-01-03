using MC_server.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MC_server.Core.Services
{
    public class RoomService
    {
        private readonly ApplicationDbContext _dbContext;

        public RoomService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Room> CreateRoomAsync(Room room)
        {
            _dbContext.Rooms.Add(room);
            await _dbContext.SaveChangesAsync();
            return room;
        }

        public async Task<Room?> GetRoomByIdAsync(int roomId)
        {
            return await _dbContext.Rooms.FindAsync(roomId);
        }

        public async Task<List<Room>> GetAllRoomsAsync()
        {
            return await _dbContext.Rooms.ToListAsync();
        }

        public async Task DeleteRoomAsync(int roomId)
        {
            Room? room = await GetRoomByIdAsync(roomId);
            if (room != null)
            {
                _dbContext.Rooms.Remove(room);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
