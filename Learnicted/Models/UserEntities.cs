namespace Learnicted.Models
{
    public class UserUnit
    {
        public int Id { get; set; }
        public int UserId { get; set; } // int ise int, string ise string yapmalısın. user.Id neyse o olmalı.
        public string UnitName { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class UserCourse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CourseName { get; set; }
        public bool IsFinished { get; set; }
    }

    public class UserRemediation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TopicName { get; set; }
        public bool IsSolved { get; set; }
    }
}