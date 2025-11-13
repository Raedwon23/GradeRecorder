namespace GradesProject
{
    public class Course
    {
        public string Code { get; set; }
        public List<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
}
