using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D424.Classes
{
    [Table("Courses")]
    public class Course
    {

        [PrimaryKey, AutoIncrement]
        public int CourseId { get; set; }
        public int TermId { get; set; }
        public int InstructorId { get; set; }
        public string CourseName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Status { get; set; }
        public string CourseDetails { get; set; }
        public int PerformanceAssessment { get; set; }
        public int ObjectiveAssessment { get; set; }
        public int StartNotification { get; set; }
        public int EndNotification { get; set; }

        // Parameterless constructor for SQLite
        public Course() { }

        // Constructor with parameters
        public Course(int termId, int instructorId, string courseName, DateTime start, DateTime end, string status, string courseDetails, int performanceAssessment, int objectiveAssessment)
        {
            TermId = termId;
            InstructorId = instructorId;
            CourseName = courseName;
            Start = start;
            End = end;
            Status = status;
            CourseDetails = courseDetails;
            PerformanceAssessment = performanceAssessment;
            ObjectiveAssessment = objectiveAssessment;
        }
    }

    [Table("Assessments")]
    public class Assessment
    {
        [PrimaryKey, AutoIncrement]
        public int AssessmentId { get; set; }

        public int Type { get; set; }
        public string AssessmentName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int StartNotification { get; set; }
        public int EndNotification { get; set; }
        public string AssessmentDetails { get; set; }
        public int CourseId { get; set; }
        public DateTime DueDate { get; set; }

        // Parameterless constructor for SQLite
        public Assessment() { }

        // Constructor with parameters
        public Assessment(int type, string assessmentName, DateTime start, DateTime end, string assessmentDetails, int courseId, int startNotification = 0, int endNotification = 0)
        {
            Type = type;
            AssessmentName = assessmentName;
            Start = start;
            End = end;
            AssessmentDetails = assessmentDetails;
            CourseId = courseId;
            StartNotification = startNotification;
            EndNotification = endNotification;
            DueDate = end; // Assuming due date is the same as the end date
        }

    }

    [Table("Notes")]
    public class Note
    {
        [PrimaryKey, AutoIncrement]
        public int NoteId { get; set; }
        public int CourseId { get; set; }
        public string Content { get; set; }

        // Parameterless constructor for SQLite
        public Note() { }

        // Constructor with parameters
        public Note(int courseId, string content)
        {
            CourseId = courseId;
            Content = content;
        }
    }

    [Table("Instructors")]
    public class Instructor
    {
        [PrimaryKey, AutoIncrement]
        public int InstructorId { get; set; }

        public string InstructorName { get; set; }
        public string InstructorPhone { get; set; }
        public string InstructorEmail { get; set; }

        // Parameterless constructor for SQLite
        public Instructor() { }

        // Constructor with parameters
        public Instructor(string instructorName, string instructorPhone, string instructorEmail)
        {
            InstructorName = instructorName;
            InstructorPhone = instructorPhone;
            InstructorEmail = instructorEmail;
        }
    }

    [Table("Terms")]
    public class Term
    {

        [PrimaryKey, AutoIncrement]
        public int TermId { get; set; }
        public string TermName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        // Parameterless constructor for SQLite
        public Term() { }

        // Constructor with parameters
        public Term(string termName, DateTime start, DateTime end)
        {
            TermName = termName;
            Start = start;
            End = end;
        }
    }

}
