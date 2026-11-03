namespace KulcsRendszer.DataContext.Dtos {
    public class RoomResponse {

        public string Id { get; set; } = "";
        public string Building { get; set; } = "";
        public int FloorId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string Equipment { get; set; } = "";
    }

    public class RoomCreateDto {
        public string Id { get; set; } = "";
        public int FloorId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string Equipment { get; set; } = "";
    }

    public class RoomUpdateDto {
        public int FloorId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string Equipment { get; set; } = "";
    }

    public class RoomInfoDto {
        public string Id { get; set; } = "";
        public int FloorId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string Equipment { get; set; } = "";
    }
}
