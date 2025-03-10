# Wonder RSTB Gen
This tool is a small generator for size tables, intended for mods. This tool is made with SMB Wonder v1.0.0 in mind, but might work for other games as well.

## Prerequisits
You need to have .NET 8.0 installed.

## Running the executable
The compiled tool consists of a single executable (.exe). Place it inside your mods' ``romfs`` folder. Make sure you have an existing size table ``romfs/System/Resource/ResourceSizeTable.Product.100.rsizetable.zs`` and run the executable. Let it finish and that's it!

**WARNING:** Beware that this is still an experimental tool. I recommend making a backup of your size table before running it.

## Building from source
To compile the tool into a single executable, open the Visual Studio solution (I recommend using Visual Studio 2022), go to the project file browser and right click on the root ``Wonder RSTB Gen``. Select ``Publish`` and a new tab should appear. At the very top will be a button called ``Publish``, pressing that will build the executable inside ``bin/[Debug or Release]/net8.0/publish/[in my case win-x86]/``. That is the executable you want to use.