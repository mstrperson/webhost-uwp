using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebhostMySQLConnection.Web;

namespace WebhostMySQLConnection
{
    public class DateRange:IComparable<DateRange>
    {
        #region Static Methods

        public static DateRange ThisAttendanceWeek
        {
            get
            {
                return new DateRange(ThisFriday.AddDays(-7), ThisFriday.AddDays(-1));
            }
        }

        /// <summary>
        /// Gets the current Academic Year.
        /// Year break is 7 June, since Graduation is in the First Week of June at the latest.
        /// By design, this is also the UniqueID for the Current Academic year Object in the Database.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentAcademicYear()
        {
            int termId = GetCurrentOrLastTerm();
            using(WebhostEntities db = new WebhostEntities())
            {
                return db.Terms.Where(t => t.id == termId).Single().AcademicYearID;
            }
            //return DateTime.Today.Year + (DateTime.Today.Month > 6 || (DateTime.Today.Month == 6 && DateTime.Today.Day > 10) ? 1 : 0);
        }

        /// <summary>
        /// Gets the currently active term (or the term that just ended, if no term is active.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentOrLastTerm()
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                foreach (Term term in db.Terms)
                {
                    if ((new DateRange(term.StartDate, term.EndDate)).Contains(DateTime.Today))
                        return term.id;
                }

                // Find the Last Term that Ended.
                return db.Terms.Where(t => t.EndDate < DateTime.Today).OrderBy(t => t.EndDate).ToList().Last().id;
            }
        }

        /// <summary>
        /// Get the id of the Next term.  If none exists returns -1.
        /// </summary>
        /// <returns></returns>
        public static int GetNextTerm()
        {
            int thisTermId = GetCurrentOrLastTerm();
            using (WebhostEntities db = new WebhostEntities())
            {
                List<int> terms = db.Terms.OrderBy(t => t.StartDate).Select(t => t.id).ToList();
                try
                {
                    return terms[terms.IndexOf(thisTermId) + 1];
                }
                catch(IndexOutOfRangeException)
                {
                    return -1;
                }
            }
        }

        #region Block Time Calculations
        
        public static String BlockOrderByDayOfWeek(DateTime date)
        {
            String blocks = "ABCDEF";

            if(date.DayOfWeek == DayOfWeek.Wednesday)
            {
                using(WebhostEntities db = new WebhostEntities())
                {
                    if (db.WednesdaySchedules.Where(w => w.Day.Equals(date.Date)).Count() <= 0)
                    {
                        WebhostEventLog.Syslog.LogWarning("I don't know if today is an ABC wednesday or not!");
                        try
                        {
                            MailControler.MailToWebmaster("Wednesday Schedule?", "I don't know if today is ABC or not >_<");
                        }
                        catch
                        {
                            WebhostEventLog.Syslog.LogError("I couldn't send you an email to let you know...");
                        }
                        return blocks;
                    }

                    WednesdaySchedule wed = db.WednesdaySchedules.Where(w => w.Day.Equals(date.Date)).Single();
                    if (wed.IsABC)
                        return "ABC";

                    return "DEF";
                }
            }

            int offset = 0;
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Tuesday: offset = 1; break;
                case DayOfWeek.Thursday: offset = 2; break;
                case DayOfWeek.Friday: offset = 3; break;
                default: offset = 0; break;
            }
            String ordered = "";
            for (int i = 0; i < 6; i++)
            {
                ordered += blocks[(i + offset) % 6];
            }

            return ordered;
        }

        public static Dictionary<DateRange, int> BlockIdsByTime(DateTime date)
        {
            Dictionary<DateRange, int> dict = new Dictionary<DateRange, int>();
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) return dict;
            int year = GetCurrentAcademicYear();

            String BlockOrderByDay = BlockOrderByDayOfWeek(date);
            using (WebhostEntities db = new WebhostEntities())
            {
                List<Block> blocks = db.Blocks.Where(b => b.AcademicYearID == year).ToList();
                dict.Add(new DateRange(DateTime.Today.AddHours(8), DateTime.Today.AddHours(8).AddMinutes(30)), blocks.Where(b => b.Name.Equals("Morning Meeting")).Single().id);
                dict.Add(FirstBlock(date), blocks.Where(b => b.Name.Equals("" + BlockOrderByDay[0])).Single().id);
                dict.Add(SecondBlock(date), blocks.Where(b => b.Name.Equals("" + BlockOrderByDay[1])).Single().id);
                dict.Add(ThirdBlock(date), blocks.Where(b => b.Name.Equals("" + BlockOrderByDay[2])).Single().id);
                if (date.DayOfWeek != DayOfWeek.Wednesday)
                {
                    dict.Add(FourthBlock(date), blocks.Where(b => b.Name.Equals("" + BlockOrderByDay[3])).Single().id);
                    dict.Add(FifthBlock(date), blocks.Where(b => b.Name.Equals("" + BlockOrderByDay[4])).Single().id);
                    dict.Add(SixthBlock(date), blocks.Where(b => b.Name.Equals("" + BlockOrderByDay[5])).Single().id);
                }
                dict.Add(new DateRange(date.AddHours(15), date.AddHours(17).AddMinutes(30)), blocks.Where(b => b.Name.Equals("Sports")).Single().id);
                if (DateTime.Today.DayOfWeek != DayOfWeek.Friday)
                {
                    if(DateTime.Today.DayOfWeek == DayOfWeek.Monday || DateTime.Today.DayOfWeek == DayOfWeek.Wednesday)
                        dict.Add(new DateRange(date.AddHours(19), date.AddHours(21)), blocks.Where(b => b.Name.Equals("MEAS")).Single().id);
                    else
                        dict.Add(new DateRange(date.AddHours(19), date.AddHours(21)), blocks.Where(b => b.Name.Equals("TEAS")).Single().id);
                    dict.Add(new DateRange(date.AddHours(19).AddMinutes(30), date.AddHours(21).AddMinutes(30)), blocks.Where(b => b.Name.Equals("Study Hall")).Single().id);
                }
            }
            return dict;
        }

        public static DateRange FirstBlock(DateTime dt)
        {
            if (dt.DayOfWeek == DayOfWeek.Thursday)
            {
                dt = dt.AddHours(9).AddMinutes(30);
            }
            else if (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
            {
                dt = dt.AddHours(8).AddMinutes(30);
            }
            else
            {
                throw new InvalidOperationException("No classes meet on the weekend.");
            }

            return new DateRange(dt, dt.AddMinutes(45));
        }

        public static DateRange SecondBlock(DateTime date)
        {
            return FirstBlock(date).MoveByMinutes(50);
        }

        public static DateRange ThirdBlock(DateTime date)
        {
            return FirstBlock(date).MoveByMinutes(100);
        }

        public static DateRange FourthBlock(DateTime dt)
        {
            if (dt.DayOfWeek == DayOfWeek.Wednesday)
            {
                throw new InvalidOperationException("No afternoon classes on Wednesday.");
            }

            if (dt.DayOfWeek == DayOfWeek.Thursday)
            {
                dt = dt.AddHours(13).AddMinutes(10);
            }
            else if (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
            {
                dt = dt.AddHours(12).AddMinutes(10);
            }
            else
            {
                throw new InvalidOperationException("No classes meet on the weekend.");
            }

            return new DateRange(dt, dt.AddMinutes(45));
        }

        public static DateRange FifthBlock(DateTime date)
        {
            return FourthBlock(date).MoveByMinutes(50);
        }

        public static DateRange SixthBlock(DateTime date)
        {
            return FourthBlock(date).MoveByMinutes(100);
        }



        #endregion

        public static DateRange Detention
        {
            get
            {
                return new DateRange(ThisFriday.AddHours(19).AddMinutes(30), ThisFriday.AddHours(21).AddHours(30));
            }
        }

        /// <summary>
        /// If it is the weekend, gets the Friday starting the weekend.
        /// If it is NOT the weekend, gets the NEXT friday.
        /// </summary>
        public static DateTime ThisFriday
        {
            get
            {
                return FridayOf(DateTime.Today);
            }
        }

        /// <summary>
        /// If date is on the weekend, gets the Friday starting that weekend.
        /// Otherwise gets the NEXT Friday.
        /// </summary>
        /// <param name="date">Selected Date.</param>
        /// <returns></returns>
        public static DateTime FridayOf(DateTime date)
        {
            if (date.DayOfWeek != DayOfWeek.Friday && WeekendDays.Contains(date.DayOfWeek))
            {
                for (DateTime day = date; day.DayOfWeek != DayOfWeek.Thursday; day = day.AddDays(-1))
                    if (day.DayOfWeek == DayOfWeek.Friday) return day;
            }
            else if (date.DayOfWeek != DayOfWeek.Friday)
            {
                for (DateTime day = date; day.DayOfWeek != DayOfWeek.Saturday; day = day.AddDays(1))
                    if (day.DayOfWeek == DayOfWeek.Friday) return day;
            }

            return date;
        }

        /// <summary>
        /// Friday, Saturday, Sunday!
        /// </summary>
        public static List<DayOfWeek> WeekendDays = new List<DayOfWeek>() { DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        /// <summary>
        /// Dictionary mapping Names of the days of the week to the Enum.
        /// </summary>
        public static Dictionary<String, DayOfWeek> DaysOfWeek = new Dictionary<string, DayOfWeek>() 
            { { "Monday", DayOfWeek.Monday }, 
              { "Tuesday", DayOfWeek.Tuesday }, 
              { "Wednesday", DayOfWeek.Wednesday }, 
              { "Thursday", DayOfWeek.Thursday }, 
              { "Friday", DayOfWeek.Friday }, 
              { "Saturday", DayOfWeek.Saturday }, 
              { "Sunday", DayOfWeek.Sunday } };

        /// <summary>
        /// Convert a MM/DD/YYYY String to a DateTime Object.
        /// </summary>
        /// <param name="dateStr"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeFromString(String dateStr)
        {
            try
            {
                String[] parts = dateStr.Split('/');
                return new DateTime(Convert.ToInt32(parts[2]), Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
            }
            catch (Exception e)
            {
                throw new InvalidCastException("Failed to parse date string " + dateStr, e);
            }
        }

        /// <summary>
        /// Searches for the current (or next) weekend.
        /// If no weekend is saved for that weekend, returns -1.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentWeekendId()
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                int year = GetCurrentAcademicYear();
                if (db.Weekends.Where(w => w.StartDate.Equals(ThisFriday)).Count() > 0)
                    return db.Weekends.Where(w => w.StartDate.Equals(ThisFriday)).Single().id;

                return -1;
            }
        }

        #endregion // Static Methods

        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        /// <summary>
        /// Represent a range of Dates from start to end, inclusively.
        /// </summary>
        /// <param name="Start"></param>
        /// <param name="End"></param>
        public DateRange(DateTime Start, DateTime End)
        {
            if (Start > End)
            {
                DateTime temp = Start;
                Start = End;
                End = temp;
            }
            this.Start = Start;
            this.End = End;
        }

        /// <summary>
        /// Gets a particular day of the week from the date range.
        /// </summary>
        /// <param name="day">Which day of the week.</param>
        /// <param name="count">How many? (default: 1 -> First)</param>
        /// <returns></returns>
        public DateTime GetDayOfWeek(String day, int count = 1)
        {
            if (DaysOfWeek.ContainsKey(day))
            {
                return GetDayOfWeek(DaysOfWeek[day], count);
            }

            throw new InvalidOperationException(String.Format("Invalid DayOfWeek {0}.", day));
        }

        /// <summary>
        /// Gets a particular day of the week from the date range.
        /// </summary>
        /// <param name="day">Which day of the week.</param>
        /// <param name="count">How many? (default: 1 -> First)</param>
        /// <returns></returns>
        public DateTime GetDayOfWeek(DayOfWeek day, int count = 1)
        {
            int i = 0;
            for (DateTime dt = Start; dt <= End; dt = dt.AddDays(1))
            {
                if (dt.DayOfWeek.Equals(day) && ++i == count)
                    return dt;
            }

            throw new InvalidOperationException(String.Format("Found {0} instances of {1} in the DateRange.  Not {2}.", i, day, count));
        }

        /// <summary>
        /// Check to see if a given date is in this DateRange.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public bool Contains(DateTime datetime)
        {
            return datetime >= Start && datetime <= End;
        }

        /// <summary>
        /// Check if this DateRange contains either of the endpoints,
        /// or if this range is inbetween the given dates.
        /// </summary>
        /// <param name="StartDate"></param>
        /// <param name="EndDate"></param>
        /// <returns></returns>
        public bool Intersects(DateTime StartDate, DateTime EndDate)
        {
            return (this.Contains(StartDate) || this.Contains(EndDate)) ||
                   (this.Start > StartDate && this.End < EndDate);
        }

        /// <summary>
        /// Checks if this DateRange contains some dates in another.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Intersects(DateRange other)
        {
            return this.Start <= other.End && other.Start <= this.End;
        }

        /// <summary>
        /// Get a full listing of dates in this range.
        /// </summary>
        /// <returns></returns>
        public List<DateTime> ToList()
        {
            List<DateTime> dates = new List<DateTime>();
            for (DateTime date = Start; date <= End; date = date.AddDays(1))
            {
                dates.Add(date);
            }

            return dates;
        }

        /// <summary>
        /// String of the form 
        /// "MM/DD/YYYY ~ MM/DD/YYYY"
        /// suitable for printing to Logs.
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            return String.Format("{0} ~ {1}", Start.ToShortDateString(), End.ToShortDateString());
        }

        /// <summary>
        /// Only Able to compare obj of type DateRange.
        /// 
        /// if one date-range is a subset of the other, returns zero.
        /// returns -1 if THIS range is earlier than OTHER,
        /// returns 1 if THIS range comes after OTHER.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(DateRange other)
        {
            if (this.Start < other.Start)
            {
                if (this.End < other.End) return -1;
                else return 0;
            }
            else
            {
                if (this.End <= other.End) return 0;
                else return 1;
            }
        }

        /// <summary>
        /// Adds the given number of minutes to both the start and end times.
        /// Returns the result as a new instance of a DateRange
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public DateRange MoveByMinutes(int minutes)
        {
            return new DateRange(this.Start.AddMinutes(minutes), this.End.AddMinutes(minutes));
        }

        /// <summary>
        /// Adds the given number of hours to both the start and end times.
        /// Returns the result as a new instance of a DateRange
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public DateRange MoveByHours(int hours)
        {
            return new DateRange(this.Start.AddHours(hours), this.End.AddHours(hours));
        }

        /// <summary>
        /// Adds the given number of days to both the start and end times.
        /// Returns the result as a new instance of a DateRange
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public DateRange MoveByDays(int days)
        {
            return new DateRange(this.Start.AddDays(days), this.End.AddDays(days));
        }
    }
}
