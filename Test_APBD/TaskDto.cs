public class TaskDto
    {
        public int IdProject { get; set; }
        public int IdTaskType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public int IdAssignedTo { get; set; }
        public int IdCreator { get; set; }
    }