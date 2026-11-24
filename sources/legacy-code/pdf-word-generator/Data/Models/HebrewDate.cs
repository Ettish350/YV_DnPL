using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Data.Models
{
    public class HebrewDate
    {
        private static string GetDate(int year,int month,int day,bool isLeapYear)
        {
            string sYear = string.Empty;
            string sMonth = string.Empty;
            string sDay = string.Empty;
            switch (year)
            {
                case 5781:
                    sYear = "תשפ\"א";
                    break;
                case 5782:
                    sYear = "תשפ\"ב";
                    break;
                case 5783:
                    sYear = "תשפ\"ג";
                    break;
                case 5784:
                    sYear = "תשפ\"ד";
                    break;
                case 5785:
                    sYear = "תשפ\"ה";
                    break;
                case 5786:
                    sYear = "תשפ\"ו";
                    break;
                case 5787:
                    sYear = "תשפ\"ז";
                    break;
                case 5788:
                    sYear = "תשפ\"ח";
                    break;
                case 5789:
                    sYear = "תשפ\"ט";
                    break;
                case 5790:
                    sYear = "תש\"צ";
                    break;
                default:
                    sYear = year+"שנה לא תקינה";
                    break;
            }
            sMonth = getMonth(month,isLeapYear);
            switch (day)
            {
                case 1:
                    sDay = "א";
                    break;
                case 2:
                    sDay = "ב";
                    break;
                case 3:
                    sDay = "ג";
                    break;
                case 4:
                    sDay = "ד";
                    break;
                case 5:
                    sDay = "ה";
                    break;
                case 6:
                    sDay = "ו";
                    break;
                case 7:
                    sDay = "ז";
                    break;
                case 8:
                    sDay = "ח";
                    break;
                case 9:
                    sDay = "ט";
                    break;
                case 10:
                    sDay = "י";
                    break;
                case 11:
                    sDay = "י\"א";
                    break;
                case 12:
                    sDay = "י\"ב";
                    break;
                case 13:
                    sDay = "י\"ג";
                    break;
                case 14:
                    sDay = "י\"ד";
                    break;
                case 15:
                    sDay = "ט\"ו";
                    break;
                case 16:
                    sDay = "ט\"ז";
                    break;
                case 17:
                    sDay = "י\"ז";
                    break;
                case 18:
                    sDay = "י\"ח";
                    break;
                case 19:
                    sDay = "י\"ט";
                    break;
                case 20:
                    sDay = "כ";
                    break;
                case 21:
                    sDay = "כ\"א";
                    break;
                case 22:
                    sDay = "כ\"ב";
                    break;
                case 23:
                    sDay = "כ\"ג";
                    break;
                case 24:
                    sDay = "כ\"ד";
                    break;
                case 25:
                    sDay = "כ\"ה";
                    break;
                case 26:
                    sDay = "כ\"ו";
                    break;
                case 27:
                    sDay = "כ\"ז";
                    break;
                case 28:
                    sDay = "כ\"ח";
                    break;
                case 29:
                    sDay = "כ\"ט";
                    break;
                case 30:
                    sDay = "ל";
                    break;

                default:
                    sDay = day + "יום לא תקין";
                    break;
            }
            string result = sDay + " " + sMonth + " " + sYear;
            return result;
        }
        public static string GetHebDateByDate(string date)
        {
            try
            {

           
            var hebCalender = new HebrewCalendar();
           
            DateTime datetime = DateTime.ParseExact(date, "dd/MM/yyyy",null);
            var month = hebCalender.GetMonth(datetime);
            var year = hebCalender.GetYear(datetime);
            var day = hebCalender.GetDayOfMonth(datetime);
            bool isLeapYear = hebCalender.IsLeapYear(year);
            return GetDate(year, month, day, isLeapYear);
            }
            catch (Exception)
            {

                return string.Empty;

            }
        }

        public static string getMonth(int month, bool isLeapYear)
        {
            if (isLeapYear)//שנה מעוברת 
            {
                switch (month)
                {
                    case 1: return "תשרי";
                    case 2: return "חשוון";
                    case 3: return "כסליו";
                    case 4: return "טבת";
                    case 5: return "שבט";
                    case 6: return "אדר א";
                    case 7: return "אדר ב";
                    case 8: return "ניסן";
                    case 9: return "אייר";
                    case 10: return "סיון";
                    case 11: return "תמוז";
                    case 12: return "אב";
                    case 13: return "אלול";
                    default: return month + "חודש לא תקין";

                }
            }
            else
            {
                switch (month)
                {
                    case 1: return "תשרי";
                    case 2: return "חשוון";
                    case 3: return "כסליו";
                    case 4: return "טבת";
                    case 5: return "שבט";
                    case 6: return "אדר";
                    case 7: return "ניסן";
                    case 8: return "אייר";
                    case 9: return "סיון";
                    case 10: return "תמוז";
                    case 11: return "אב";
                    case 12: return "אלול";
                    default: return month + "חודש לא תקין";

                }
            }
        }
    }
}
