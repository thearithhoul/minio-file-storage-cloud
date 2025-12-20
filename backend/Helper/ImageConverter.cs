
namespace Backend.Helper
{
    public static class ImageConverter
    {
        public static byte[] DecodeBase64Image(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new FormatException("Empty base64");

            // Handle data:image/...;base64,
            var commaIndex = input.IndexOf(',');
            if (commaIndex >= 0)
                input = input.Substring(commaIndex + 1);

            return Convert.FromBase64String(input);
        }
    }
}