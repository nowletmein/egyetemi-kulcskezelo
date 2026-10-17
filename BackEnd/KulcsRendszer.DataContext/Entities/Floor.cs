namespace KulcsRendszer.DataContext.Entities {
    public class Floor {
        public int Id { get; set; }
        public int Number { get; set; }

        public ICollection<ClassRoom> Rooms { get; set; } = new List<ClassRoom>();
    }
}
