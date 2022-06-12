using System.Text;

namespace MensattScraper.Tests;

public class RandomUtil
{
    private static readonly char[] Alphabet =
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
        'W',
        'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
        't',
        'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', '!', '§', '€'
    };

    private static readonly Random Rng = new(0xae75a77);

    internal static string? GenerateRandomString(uint length)
    {
        var resultBuilder = new StringBuilder((int) length);
        for (uint i = 0; i < length; i++)
            resultBuilder.Append(Alphabet[Rng.Next(0, Alphabet.Length)]);
        return resultBuilder.ToString();
    }
}