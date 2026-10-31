namespace OrderProcessing.Shared.Exceptions;

public class DataFileCorruptedException : Exception
{
    public DataFileCorruptedException(string filename)
        : base($"Data File {filename} is corrupted")
    {
        
    }
}