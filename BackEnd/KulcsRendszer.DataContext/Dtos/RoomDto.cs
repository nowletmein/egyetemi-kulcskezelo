namespace KulcsRendszer.DataContext.Dtos {
    public class RoomResponse {

        public string Id { get; set; } = "";
        public string Building { get; set; } = "";
        public int FloorId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string Equipment { get; set; } = "";

        public RoomResponse() { }

        public RoomResponse(string id, string building, int floorId, string name, int capacity, string equipment) {
            Id = id;
            Building = building;
            FloorId = floorId;
            Name = name;
            Capacity = capacity;
            Equipment = equipment;
        }
    }
}