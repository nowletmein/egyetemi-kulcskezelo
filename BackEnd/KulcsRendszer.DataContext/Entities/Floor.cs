namespace KulcsRendszer.DataContext.Entities {
    public class Floor {
        public int Id { get; set; }
        public int Number { get; set; }

        public int BuildingId { get; set; }
        public Building Building { get; set; } = null!;

        public ICollection<ClassRoom> Rooms { get; set; } = new List<ClassRoom>();
    }
}
