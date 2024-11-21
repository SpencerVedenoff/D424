using SQLite;
using D424.Classes;
using Plugin.LocalNotification;

namespace D424;

public partial class ClassView : ContentPage
{
    public Course currentCourse;
    public Instructor currentInstructor;
    public Assessment PA, OA;
    public SQLiteConnection db = new SQLiteConnection(MainPage.databasePath);
    public static IList<NotificationRequest> notificationRequestsStatic = new List<NotificationRequest>();

    public ClassView(int courseId)
    {
        InitializeComponent();

        // Retrieve the current course and related data
        currentCourse = MainPage.courseList[courseId];
        currentInstructor = MainPage.instructors[currentCourse.InstructorId];
        PA = MainPage.assessments[currentCourse.PerformanceAssessment];
        OA = MainPage.assessments[currentCourse.ObjectiveAssessment];

        // Set course details in the UI
        courseTitle.Text = currentCourse.CourseName;
        courseStart.Date = currentCourse.Start;
        courseEnd.Date = currentCourse.End;

        // Set course status and notifications
        courseStatus.ItemsSource = MainPage.statusValues;
        courseStatus.SelectedItem = currentCourse.Status;

        courseStartNotif.ItemsSource = MainPage.notificationValues;
        courseStartNotif.SelectedItem = currentCourse.StartNotification;

        courseEndPing.ItemsSource = MainPage.notificationValues;
        courseEndPing.SelectedItem = currentCourse.EndNotification;

        // Set performance and objective assessments details
        SetAssessmentDetails(PA, OA);

        // Set instructor details
        instructorName.Text = currentInstructor.InstructorName;
        instructorPhone.Text = currentInstructor.InstructorPhone;
        instructorEmail.Text = currentInstructor.InstructorEmail;

        // Set course details text
        courseDetails.Text = currentCourse.CourseDetails;


        // Load course notes
        InputNotes();
    }

    private void SetAssessmentDetails(Assessment performanceAssessment, Assessment objectiveAssessment)
    {
        // Set date and notification values for performance assessment
        paStart.Date = performanceAssessment.Start;
        paEnd.Date = performanceAssessment.End;
        paDueDate.Date = performanceAssessment.DueDate;
        paStartNotif.ItemsSource = MainPage.notificationValues;
        paEndNotif.ItemsSource = MainPage.notificationValues;
        paStartNotif.SelectedItem = performanceAssessment.StartNotification;
        paEndNotif.SelectedItem = performanceAssessment.EndNotification;
        paName.Text = performanceAssessment.AssessmentName;

        // Set date and notification values for objective assessment
        oaStart.Date = objectiveAssessment.Start;
        oaEnd.Date = objectiveAssessment.End;
        oaDueDate.Date = objectiveAssessment.DueDate;
        oaStartNotif.ItemsSource = MainPage.notificationValues;
        oaEndNotif.ItemsSource = MainPage.notificationValues;
        oaStartNotif.SelectedItem = objectiveAssessment.StartNotification;
        oaEndNotif.SelectedItem = objectiveAssessment.EndNotification;
        oaName.Text = objectiveAssessment.AssessmentName;
    }

    public async Task ShareText(string text)
    {
        await Share.Default.RequestAsync(new ShareTextRequest { Text = text });
    }

    private void InputNotes()
    {
        // Clear existing notes from the UI stack
        noteStack.Children.Clear();

        // Loop through each note associated with the current course
        foreach (var note in MainPage.notes.Values)
        {
            if (note.CourseId == currentCourse.CourseId)
            {
                // Create swipe actions for sharing and deleting the note
                var swipeActions = CreateSwipeActions(note);

                // Create the layout for each note
                var noteLayout = CreateNoteLayout(note, swipeActions);

                // Add the layout to the note stack
                noteStack.Add(noteLayout);
            }
        }
    }

    // Helper method to create swipe actions for a note
    private List<SwipeItem> CreateSwipeActions(Note note)
    {
        // Create a share action
        var shareItem = new SwipeItem
        {
            Text = "Share",
            BindingContext = note,
            BackgroundColor = Colors.LightBlue
        };
        shareItem.Invoked += OnShare;

        // Create a delete action
        var deleteItem = new SwipeItem
        {
            Text = "Delete",
            BindingContext = note,
            BackgroundColor = Colors.LightCoral
        };
        deleteItem.Invoked += OnDel;

        return new List<SwipeItem> { shareItem, deleteItem };
    }

    // Helper method to create the layout for a note
    private SwipeView CreateNoteLayout(Note note, List<SwipeItem> swipeActions)
    {
        // Create a grid layout for the note content
        var grid = new Grid
        {
            BackgroundColor = Colors.LightBlue
        };

        // Add the note content to the grid
        grid.Add(new Label
        {
            Text = note.Content
        });

        // Create a SwipeView to hold the swipe actions and the grid
        return new SwipeView
        {
            RightItems = new SwipeItems(swipeActions),
            Content = grid
        };
    }

    private void OnDel(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.BindingContext is Note note)
        {
            // Delete the note from the database
            DB.DeleteNote(note);

            // Sync the database to reflect changes
            MainPage.Sync_DB();

            // Refresh the notes displayed in the UI
            InputNotes();
        }
    }

    private async void OnShare(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.BindingContext is Note note)
        {
            // Share the content of the note
            await ShareText(note.Content);

            // Sync the database to reflect any changes
            MainPage.Sync_DB();
        }
    }

    private void ClassStart_Date(object sender, DateChangedEventArgs e)
    {
        // Check if the new start date is valid by comparing with the end date
        if (MainPage.CheckDates(courseStart.Date, courseEnd.Date))
        {
            // Update the current course start date
            currentCourse.Start = e.NewDate;

            // Update the course in the database
            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                DB.Update_Course(db, currentCourse);
            }

            // Sync the database after the update
            MainPage.Sync_DB();
        }
    }

    private void CourseEnd_Date(object sender, DateChangedEventArgs e)
    {
        // Check if the new end date is valid by comparing with the start date
        if (MainPage.CheckDates(courseStart.Date, courseEnd.Date))
        {
            // Update the current course end date
            currentCourse.End = e.NewDate;

            // Update the course in the database
            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                DB.Update_Course(db, currentCourse);
            }

            // Sync the database after the update
            MainPage.Sync_DB();
        }
    }

    private void Course_StatusSelected(object sender, EventArgs e)
    {
        // Update the current course status based on the selected item
        currentCourse.Status = courseStatus.SelectedItem as string;

        // Update the course in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Course(db, currentCourse);
        }

        // Sync the database after the update
        MainPage.Sync_DB();
    }

    private async void CourseTitle_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Check if the new text value is null or empty
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            // Display a warning alert to the user
            await DisplayAlert("Invalid Title", "Course title cannot be empty. Please enter a valid title.", "OK");

            // Optionally, reset the title to the previous value or a default value
            courseTitle.Text = currentCourse.CourseName;
            return;
        }

        // Update the current course title based on the text input
        currentCourse.CourseName = e.NewTextValue;

        // Update the course in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Course(db, currentCourse);
        }

        // Sync the database after the update
        MainPage.Sync_DB();
    }

    private void InstructorEmail_ChangedCheck(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            DisplayAlert("Warning", "Email cannot be empty or whitespace.", "OK");

            // Revert to previous email if null or whitespace detected
            ((Entry)sender).Text = currentInstructor.InstructorEmail;
        }
        else
        {
            currentInstructor.InstructorEmail = e.NewTextValue;

            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                // Update only for the current course-instructor relationship
                DB.Update_CourseInstructor(db, currentCourse, currentInstructor);
            }

            MainPage.Sync_CurrentCourse(currentCourse.CourseId); // Pass courseId to only sync relevant data
        }
    }

    private void InstructorPhone_ChangedCheck(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            DisplayAlert("Warning", "Phone number cannot be empty or whitespace.", "OK");

            // Revert to previous phone if null or whitespace detected
            ((Entry)sender).Text = currentInstructor.InstructorPhone;
        }
        else
        {
            currentInstructor.InstructorPhone = e.NewTextValue;

            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                // Update only for the current course-instructor relationship
                DB.Update_CourseInstructor(db, currentCourse, currentInstructor);
            }

            MainPage.Sync_CurrentCourse(currentCourse.CourseId); // Pass courseId to only sync relevant data
        }
    }

    private void InstructorName_ChangedCheck(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            DisplayAlert("Warning", "Instructor name cannot be empty or whitespace.", "OK");

            // Revert to previous name if null or whitespace detected
            ((Entry)sender).Text = currentInstructor.InstructorName;
        }
        else
        {
            currentInstructor.InstructorName = e.NewTextValue;

            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                // Update only for the current course-instructor relationship
                DB.Update_CourseInstructor(db, currentCourse, currentInstructor);
            }

            MainPage.Sync_CurrentCourse(currentCourse.CourseId); // Pass courseId to only sync relevant data
        }
    }

    private void AddNotes(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(noteInput.Text))
        {
            Note newNote = new Note(currentCourse.CourseId, noteInput.Text);

            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                DB.Add_Note(db, newNote);
            }

            MainPage.Sync_DB();
        }

        // Clear the note input field after adding the note
        noteInput.Text = string.Empty;

        // Refresh the displayed notes
        InputNotes();
    }

    private async void CourseStartNotification_Selected(object sender, EventArgs e)
    {
        int selectedValue = Convert.ToInt32(courseStartNotif.SelectedItem);

        // Update the course's start notification setting, even if it's zero
        currentCourse.StartNotification = selectedValue;

        // Update the course in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Course(db, currentCourse);
        }

        DateTime currentTime = DateTime.Now;

        // Check if the current date matches the notification trigger date for the course start
        if (selectedValue != 0 && currentTime.Date == currentCourse.Start.AddDays(-selectedValue).Date)
        {
            // Show alert if it's the correct day for the notification
            await Application.Current.MainPage.DisplayAlert(
                $"{currentCourse.CourseName} Starting Soon",
                $"{currentCourse.CourseName} is starting soon!",
                "OK");
        }

        // Sync the database after the update
        MainPage.Sync_DB();
    }

    private async void CourseEndNotification_Selected(object sender, EventArgs e)
    {
        int selectedValue = Convert.ToInt32(courseEndPing.SelectedItem);

        // Update the course's end notification setting, even if it's zero
        currentCourse.EndNotification = selectedValue;

        // Update the course in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Course(db, currentCourse);
        }

        DateTime currentTime = DateTime.Now;
        // Show display message only if the selected value isn't zero and has changed
        if (selectedValue != 0 && currentTime.Date == currentCourse.End.AddDays(-selectedValue).Date)
        {
            // Show alert if it's the correct day for the notification
            await Application.Current.MainPage.DisplayAlert(
                $"{currentCourse.CourseName} Ending Soon",
                $"{currentCourse.CourseName} is ending soon!",
                "OK");
        }
        // Sync the database after the update
        MainPage.Sync_DB();

    }

    private void PaStart_IfSelected(object sender, EventArgs e)
    {
        // Update the Performance Assessment start notification setting based on the selected item
        PA.StartNotification = Convert.ToInt32(paStartNotif.SelectedItem);

        // Update the assessment in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Assessment(db, PA);
        }

        // Sync the database after the update
        MainPage.Sync_DB();
    }

    private void PaEnd_IfSelected(object sender, EventArgs e)
    {
        // Update the Performance Assessment end notification setting based on the selected item
        PA.EndNotification = Convert.ToInt32(paEndNotif.SelectedItem);

        // Update the assessment in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Assessment(db, PA);
        }

        // Sync the database after the update
        MainPage.Sync_DB();
    }

    private void OaStart_IfSelected(object sender, EventArgs e)
    {
        // Update the Objective Assessment start notification setting based on the selected item
        OA.StartNotification = Convert.ToInt32(oaStartNotif.SelectedItem);

        // Update the assessment in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Assessment(db, OA);
        }

        // Sync the database after the update
        MainPage.Sync_DB();
    }

    private void CourseDetails_ChangeCheck(object sender, TextChangedEventArgs e)
    {
        // Update the course details with the new text value
        currentCourse.CourseDetails = e.NewTextValue;

        // Update the course in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Course(db, currentCourse);
        }

        // Sync the database after the update
        MainPage.Sync_DB();
    }

    private void Oa_TextChangeCheck(object sender, TextChangedEventArgs e)
    {
        // Check if the new text value is not null
        if (e.NewTextValue != null)
        {
            // Update the assessment name with the new text value
            OA.AssessmentName = e.NewTextValue;

            // Update the assessment in the database
            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                DB.Update_Assessment(db, OA);
            }

            // Sync the database after the update
            MainPage.Sync_DB();
        }
    }

    private void Pa_TextChangeCheck(object sender, TextChangedEventArgs e)
    {
        // Check if the new text value is not null
        if (e.NewTextValue != null)
        {
            // Update the performance assessment name with the new text value
            PA.AssessmentName = e.NewTextValue;

            // Update the assessment in the database
            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                DB.Update_Assessment(db, PA);
            }

            // Sync the database after the update
            MainPage.Sync_DB();
        }
    }

    private void Oa_DueDateSelected(object sender, DateChangedEventArgs e)
    {
        // Update the objective assessment's due date with the selected new date
        OA.DueDate = e.NewDate;

        // Update the assessment in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            db.Update(OA);
        }

        // Sync the database after the update
        MainPage.Sync_DB();
    }

    private void Pa_DueDateSelected(object sender, DateChangedEventArgs e)
    {
        // Update the due date for the performance assessment (PA) to the new date selected
        PA.DueDate = e.NewDate;

        // Update the performance assessment in the database using the updated PA object
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            db.Update(PA);
        }

        // Sync the database to reflect the changes
        MainPage.Sync_DB();
    }

    private void Oa_StartDateSelected(object sender, DateChangedEventArgs e)
    {
        // Update the start date for the objective assessment (OA) to the new date selected
        OA.Start = e.NewDate;

        // Update the objective assessment in the database using the updated OA object
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            db.Update(OA);
        }

        // Sync the database to reflect the changes
        MainPage.Sync_DB();
    }

    private void Oa_EndDateSelected(object sender, DateChangedEventArgs e)
    {
        // Update the end date for the objective assessment (OA) to the new date selected
        OA.End = e.NewDate;

        // Update the objective assessment in the database using the updated OA object
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            db.Update(OA);
        }

        // Sync the database to reflect the changes
        MainPage.Sync_DB();
    }

    private void Pa_StartDateSelected(object sender, DateChangedEventArgs e)
    {
        // Update the start date for the performance assessment (PA) to the new date selected
        PA.Start = e.NewDate;

        // Update the performance assessment in the database using the updated PA object
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            db.Update(PA);
        }

        // Sync the database to reflect the changes
        MainPage.Sync_DB();
    }

    private void Pa_EndDateSelected(object sender, DateChangedEventArgs e)
    {
        // Update the end date for the performance assessment (PA) to the new date selected
        PA.End = e.NewDate;

        // Update the performance assessment in the database using the updated PA object
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            db.Update(PA);
        }

        // Sync the database to reflect the changes
        MainPage.Sync_DB();
    }

    private void OaNotification_Selected(object sender, EventArgs e)
    {
        // Update the Objective Assessment (OA) end notification value based on the selected item
        OA.EndNotification = Convert.ToInt32(oaEndNotif.SelectedItem);

        // Update the objective assessment in the database
        using (var db = new SQLiteConnection(MainPage.databasePath))
        {
            DB.Update_Assessment(db, OA);
        }

        // Sync the database to reflect the changes
        MainPage.Sync_DB();
    }

    private async void OnDeleteAssessmentClicked(object sender, EventArgs e)
    {
        List<int> cancelledRequests = new List<int>();

        if (sender is Button clickedButton)
        {
            if (clickedButton.ClassId == "DeletePAButton")
            {
                paName.Text = string.Empty;
                paDueDate.Date = DateTime.Now;
                paStart.Date = DateTime.Now;
                paEnd.Date = DateTime.Now;
                paStartNotif.SelectedIndex = -1;
                paEndNotif.SelectedIndex = -1;

                cancelledRequests.Add(paStartNotif.SelectedIndex);
                cancelledRequests.Add(paEndNotif.SelectedIndex);

                await DisplayAlert("Confirmation", "Performance Assessment information has been cleared.", "OK");
            }
            else if (clickedButton.ClassId == "DeleteOAButton")
            {
                oaName.Text = string.Empty;
                oaDueDate.Date = DateTime.Now;
                oaStart.Date = DateTime.Now;
                oaEnd.Date = DateTime.Now;
                oaStartNotif.SelectedIndex = -1;
                oaEndNotif.SelectedIndex = -1;

                cancelledRequests.Add(oaStartNotif.SelectedIndex);
                cancelledRequests.Add(oaEndNotif.SelectedIndex);

                await DisplayAlert("Confirmation", "Objective Assessment information has been cleared.", "OK");
            }
        }

        // Call the CancelNotifications method from MainPage
        MainPage.CancelNotifications(cancelledRequests);
    }

}