using System.Collections.Generic;

namespace KulcsRendszer.DataContext.Enums {
    public static class IssueTicketStatuses {
        public const string Open = "Open";
        public const string InProgress = "InProgress";
        public const string Resolved = "Resolved";
        public const string Closed = "Closed";

        public static readonly IReadOnlyList<string> All =
            new[] { Open, InProgress, Resolved, Closed };
    }
}
