using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Chat.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Tests.TestSupport;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options, [typeof(MessageConfiguration).Assembly])
{
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<Mute> Mutes => Set<Mute>();

    public DbSet<ChatReadState> ChatReadStates => Set<ChatReadState>();
}
