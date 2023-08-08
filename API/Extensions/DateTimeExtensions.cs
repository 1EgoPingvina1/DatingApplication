namespace API.Extensions
{
    public static class DateTimeExtensions
    {
        public static int CalculateAge(this DateTime date) 
        {
            var today = DateTime.UtcNow;

            var age = today.Year - date.Year;

            if(date > today.AddYears(-age)) 
                age--;
            return age;
        }
    }
}
