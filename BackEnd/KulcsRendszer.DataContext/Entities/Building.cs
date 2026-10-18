namespace KulcsRendszer.DataContext.Entities {
    public class Building {
        public int Id { get; set; }
        public string BuildingCode { get; set; } = "";
        public string BuildingName { get; set; } = "";

        public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    }
}
