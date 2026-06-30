namespace AskMeFirst.Core.Data;

public static class PngSignature
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];

    public static bool Matches(byte[] bytes)
    {
        if (bytes.Length < PngMagic.Length)
        {
            return false;
        }
        for (int i = 0; i < PngMagic.Length; i++)
        {
            if (bytes[i] != PngMagic[i])
            {
                return false;
            }
        }
        return true;
    }
}
