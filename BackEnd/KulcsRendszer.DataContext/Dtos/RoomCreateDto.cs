namespace KulcsRendszer.DataContext.Dtos {
    public class RoomCreateDto {
        public string Id { get; set; } = "";
        public int FloorId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string Equipment { get; set; } = "";
    }
}
