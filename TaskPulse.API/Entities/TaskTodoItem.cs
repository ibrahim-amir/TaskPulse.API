using TaskPulse.API.Entities.BaseEntityClass;

namespace TaskPulse.API.Entities
{
    // A short, self-ticked checklist item — unlike TaskSubtask (a required
    // file upload reviewed by a Manager), this is just text anyone on the
    // task ticks off themselves. Drives the task card's progress fill
    // independently of the required-file checklist.
    public class TaskTodoItem : BasicEntityClass
    {
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
        public bool IsDone { get; set; } = false;
    }
}
