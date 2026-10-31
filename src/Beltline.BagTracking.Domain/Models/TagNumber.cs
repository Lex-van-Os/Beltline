namespace Beltline.BagTracking.Domain.Models;

public class TagNumber
{
    public string Number { get; }
    
    public TagNumber(string tagNumber)
    {
        Number = ValidateTagNumber(tagNumber);
    }
    
    public static string ValidateTagNumber(string tagNumber)
    {
        ArgumentException.ThrowIfNullOrEmpty(tagNumber);
        
        foreach (var character in tagNumber)
        {
            if (!char.IsAsciiDigit(character))
                throw new ArgumentException("The given tag number contains illegal characters.", nameof(tagNumber));
        }
        
        if (tagNumber.Length != 10)
            throw new ArgumentException("The given tag number is not of the correct length (10 numbers).", nameof(tagNumber));

        return tagNumber;
    }
}