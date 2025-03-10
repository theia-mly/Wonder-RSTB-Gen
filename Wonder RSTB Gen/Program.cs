using ResourceSizeTable;
using System.IO;
using System.Reflection.Metadata;

internal class Program
{
    public const string Version = "v0.1.0";

    private static void Main(string[] args)
    {
        var rom_path = args.Length == 0 ? Environment.CurrentDirectory : args[0];
        var reL_rstb_path = Path.Combine("System", "Resource", "ResourceSizeTable.Product.101.rsizetable.zs");
        var rstb_path = Path.Combine(rom_path, reL_rstb_path);

        if (!File.Exists(rstb_path))
        {
            reL_rstb_path = Path.Combine("System", "Resource", "ResourceSizeTable.Product.100.rsizetable.zs");
            rstb_path = Path.Combine(rom_path, reL_rstb_path);

            if (!File.Exists(rstb_path))
            {
                Console.WriteLine($"Could not find RSTB in location {rstb_path}.");
                return;
            }
        }

        Console.WriteLine("RSTB found.");
        ResourceSizeTable.ResourceSizeTable rstb = new ResourceSizeTable.ResourceSizeTable(rstb_path);

        rstb.UpdateTable(rom_path);

        rstb.Save(rstb_path);

        Console.WriteLine("Size Table created. Press any key to exit.");
        Console.Read();
    }
}