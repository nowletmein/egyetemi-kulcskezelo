namespace KulcsRendszer.DataContext.Dtos {
    public record RoomResponse(
        string Id,
        string Building,
        int FloorId,
        string Name,
        int Capacity,
        string Equipment);
}
