using System;

class Program
{
    static void Main()
    {
        Console.WriteLine($"AP: {GetExcelColumnIndex("AP")}");
        Console.WriteLine($"AO: {GetExcelColumnIndex("AO")}");
    }

    public static int GetExcelColumnIndex(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return -1;
        columnName = columnName.Trim().ToUpper();
        int index = 0;
        foreach (char c in columnName)
        {
            if (c < 'A' || c > 'Z') return -1;
            index = index * 26 + (c - 'A' + 1);
        }
        return index;
    }
}
