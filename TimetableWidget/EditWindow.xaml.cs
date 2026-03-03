using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TimetableWidget
{
    public partial class EditWindow : Window
    {
        private Dictionary<string, List<Lecture>> _data = null!;
        private ObservableCollection<Lecture> _current = new();
        private string _selectedDay = "MON";

        // Called after save so the main widget can refresh
        public event System.Action? TimetableSaved;

        private static readonly string[] Days = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

        public EditWindow()
        {
            InitializeComponent();
            _data = TimetableStore.Load();

            // Ensure all days exist
            foreach (var d in Days)
                if (!_data.ContainsKey(d))
                    _data[d] = new List<Lecture>();

            DayCombo.ItemsSource = Days;
            DayCombo.SelectedIndex = 0;
        }

        private void LoadDay(string day)
        {
            _selectedDay = day;
            _current = new ObservableCollection<Lecture>(
                _data.ContainsKey(day) ? _data[day] : new List<Lecture>());
            LectureListView.ItemsSource = _current;
        }

        private void DayCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DayCombo.SelectedItem is string day)
                LoadDay(day);
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var time = TimeInput.Text.Trim();
            var subj = SubjectInput.Text.Trim();
            if (string.IsNullOrEmpty(time) || string.IsNullOrEmpty(subj)) return;

            _current.Add(new Lecture { Time = time, Subject = subj });
            _data[_selectedDay] = _current.ToList();

            TimeInput.Clear();
            SubjectInput.Clear();
            TimeInput.Focus();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LectureListView.SelectedItem is Lecture lec)
            {
                _current.Remove(lec);
                _data[_selectedDay] = _current.ToList();
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // Sync current day changes before saving
            _data[_selectedDay] = _current.ToList();
            TimetableStore.Save(_data);
            TimetableSaved?.Invoke();
            Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    }
}
