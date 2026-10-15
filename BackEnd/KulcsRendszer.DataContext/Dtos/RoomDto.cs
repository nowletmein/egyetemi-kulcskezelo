namespace KulcsRendszer.DataContext.Dtos {
    public sealed record RoomResponse(
        string Id,
        string Building,
        int FloorId,
        string Name,
        int Capacity,
        string Equipment);
}
