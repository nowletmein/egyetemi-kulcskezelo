namespace KulcsRendszer.DataContext.Dtos {
    /// <summary>
    /// Represents a room returned by the API.
    /// </summary>
    public sealed record RoomResponse(
        string Id,
        string Building,
        int FloorId,
        string Name,
        int Capacity,
        string Equipment);
}
