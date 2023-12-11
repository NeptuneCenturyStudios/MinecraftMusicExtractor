using System.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// See https://aka.ms/new-console-template for more information
// Set how many menu items there are.
const int NUM_MENU_ITEMS = 2;
const int NUM_MENU_LINES = 3;
// Currently selected menu option.
var _selectionIndex = 0;
// Stores the asset name and hash as strings.
Dictionary<string, string> _objectHash = new();
// Create a menu state object to track user's selection.
var _menuState = new MenuState()
{
    ExtractMusic = true
};

Console.WriteLine("Minecraft Music Extractor");

// Prompt user for the minecraft folder
string? _minecraftDir = null;
while (_minecraftDir == null)
{
    Console.Write("Enter location of .minecraft folder. (Example: home/.minecraft): ");
    _minecraftDir = Console.ReadLine();
    // Check to make sure the .minecraft folder that the user specified exists.
    if (!Path.Exists(_minecraftDir))
    {
        WriteLine($"The directory {_minecraftDir} does not exist or you do not have access to it.", ConsoleColor.Red);
        // Reset and try again.
        _minecraftDir = null;
    }
}

// Prompt user for the destination folder. This is where the files will be copied to.
string? _destinationDir = null;
while (_destinationDir == null)
{
    Console.Write("Specify location to copy sound files: ");
    _destinationDir = Console.ReadLine();
    // Set path
    if (_destinationDir != null)
    {
        _destinationDir = Path.Combine(_destinationDir, "MinecraftMusicExtractor");
        // Check to make sure the destination folder that the user specified exists or that we have access to create it.
        if (!Path.Exists(_destinationDir))
        {
            try
            {
                // Attempt to create it.
                Directory.CreateDirectory(_destinationDir);
            }
            catch (Exception)
            {
                WriteLine($"The directory {_destinationDir} does not exist or you do not have permission to create it.", ConsoleColor.Red);
                // Reset and try again.
                _destinationDir = null;

            }

        }
    }
}

// Present the menu.
WriteLine("\nSelect extraction options. Use Up/Down and Space to select. When ready, press Enter.", ConsoleColor.Blue);
PrintMainMenu(_menuState);

// Start main loop.
ConsoleKeyInfo _key = default;
while (_key.Key != ConsoleKey.Escape)
{
    _key = Console.ReadKey(true);

    // If key is down arrow, increase selection
    if (_key.Key == ConsoleKey.DownArrow)
    {
        MoveSelectionDown();
    }
    else if (_key.Key == ConsoleKey.UpArrow)
    {
        MoveSelectionUp();
    }
    else if (_key.Key == ConsoleKey.Spacebar)
    {
        SetSelection(_menuState);

    }
    else if (_key.Key == ConsoleKey.Enter)
    {
        // Once we have the minecraft directory and the options, look into the assets folder and find the indexes. This keeps a
        // directory of resource names and hashes. We need to look up the resource name and locate the file by its hash.
        // Load hashes into the _objectHash dictionary.
        var success = await LoadIndexesAsync();
        // Once the hash indexhas been created, extract the files from the assets folder and copy it to the new destination.
        if (success)
        {
            Console.WriteLine();
            Write("Proceed? (Y/N): ", ConsoleColor.Yellow);
            var responseKey = Console.ReadKey();
            if (responseKey.Key == ConsoleKey.Y)
            {
                await ExtractAssetsAsync();
            }
            else
            {
                WriteLine("\nAborted.", ConsoleColor.Red);
            }
        }
        else
        {
            WriteLine("Failed.", ConsoleColor.Red);
        }
        break;
    }
    else if (_key.Key == ConsoleKey.Escape)
    {
        // Quit
        WriteLine("Aborted.", ConsoleColor.Red);
        break;
    }

    // Move back to the top of the menu
    Console.CursorTop -= NUM_MENU_LINES;

    // Print the menu again
    PrintMainMenu(_menuState);
}

/// <summary>
/// Writes a line of text to the console in a specified color. Resets the color to Gray when done.
/// </summary>
void WriteLine(string text, ConsoleColor color = ConsoleColor.Gray)
{
    Console.ForegroundColor = color;
    Console.WriteLine(text);
    Console.ForegroundColor = ConsoleColor.Gray;
}

/// <summary>
/// Writes text to the console in a specified color. Resets the color to Gray when done.
/// </summary>
void Write(string text, ConsoleColor color = ConsoleColor.Gray)
{
    Console.ForegroundColor = color;
    Console.Write(text);
    Console.ForegroundColor = ConsoleColor.Gray;
}

/// <summary>
/// Prints the options menu on the screen
/// </summary>
void PrintMainMenu(MenuState menuState)
{
    // Print each menu option and update with selection
    Write($"{GetSelectionCursor(0)} ", ConsoleColor.Blue);
    Write($"[{GetSelectionCharacter(menuState.ExtractMusic)}] Extract music");
    Console.WriteLine();

    Write($"{GetSelectionCursor(1)} ", ConsoleColor.Blue);
    Write($"[{GetSelectionCharacter(menuState.ExtractMobSounds)}] Extract mob sounds");
    Console.WriteLine();

    WriteLine("Press escape to quit.");

}

/// <summary>
/// Sets the selection in the menu state
/// </summary>
void SetSelection(MenuState menuState)
{
    switch (_selectionIndex)
    {
        case 0:
            menuState.ExtractMusic = !menuState.ExtractMusic;
            break;
        case 1:
            menuState.ExtractMobSounds = !menuState.ExtractMobSounds;
            break;
    }
}

/// <summary>
/// Gets the character used to denote a checkmark
/// </summary>
static string GetSelectionCharacter(bool value)
{
    // Return an x if true or space if false
    return value ? "x" : " ";
}

/// <summary>
/// Gets the character used to denote a cursor
/// </summary>
string GetSelectionCursor(int menuIndex)
{
    return _selectionIndex == menuIndex ? ">" : " ";
}

/// <summary>
/// Moves the cursor up
/// </summary>
void MoveSelectionUp()
{
    // Increase selection index
    _selectionIndex--;
    if (_selectionIndex < 0)
    {
        // Go to the end
        _selectionIndex = NUM_MENU_ITEMS - 1;
    }
}

/// <summary>
/// Moves the cursor down
/// </summary>
void MoveSelectionDown()
{
    // Increase selection index
    _selectionIndex++;
    if (_selectionIndex > NUM_MENU_ITEMS - 1)
    {
        // Go back to beginning
        _selectionIndex = 0;
    }
}

/// <summary>
/// Loads the index files into a dictionary of asset names and their respective hashes.
/// </summary>
async Task<bool> LoadIndexesAsync()
{
    WriteLine("\nSearching for indexes...");

    var assetsFolder = Path.Combine(_minecraftDir, "assets");
    var indexesFolder = Path.Combine(assetsFolder, "indexes");
    if (Directory.Exists(indexesFolder))
    {
        // Look up the .json files in the folder.
        var indexFiles = Directory.EnumerateFiles(indexesFolder, "*.json");
        Console.WriteLine($"Reading {indexFiles.Count()} index(es)...");
        foreach (var indexFile in indexFiles)
        {
            var file = new FileInfo(indexFile);
            // Read the index and deserialize the json file.
            var indexJson = await File.ReadAllTextAsync(indexFile);
            // Deserialize to a JObject.
            var objectIndex = (JObject?)JsonConvert.DeserializeObject(indexJson);
            if (objectIndex != null)
            {
                // Add the indexes to a dictionary.
                var assets = objectIndex["objects"]?.Children();
                if (assets != null)
                {
                    decimal percentComplete = 0;
                    int index = 0;
                    int assetCount = assets.AsJEnumerable().Count();
                    foreach (var asset in assets.OfType<JProperty>())
                    {
                        // Increase index for each asset we process
                        index++;

                        var assetName = asset.Name;
                        var value = asset.Value;
                        // Get the hash property of the asset.
                        var hash = value?["hash"]?.Value<JToken>()?.ToString();
                        // We must have an asset name and a hash.
                        if (assetName != null && hash != null && !_objectHash.ContainsKey(assetName))
                        {
                            var addAsset = false;
                            // Use the menu options to decide which files are going to get extracted.
                            if ((_menuState.ExtractMusic && assetName.Contains("sounds/music"))
                                || (_menuState.ExtractMobSounds && assetName.Contains("sounds/mob")))
                            {
                                addAsset = true;
                            }

                            // Only add the asset if it is what the user wants to extract
                            if (addAsset)
                            {
                                // Add the asset name and the hash to our dictionary.
                                _objectHash.Add(assetName, hash);
                            }

                        }

                        // Track progress.
                        percentComplete = (decimal)index / assetCount;
                        Write($"Loading assets from {file.Name}. ");
                        Write($"{percentComplete:##.##%}    ", ConsoleColor.Yellow);
                        // Set cursor back to beginning of progress line.
                        Console.CursorLeft = 0;

                    }
                }


            }

            // Next index file
            Console.WriteLine();
        }

        // Check that we have any loaded assets.
        if (_objectHash.Count > 0)
        {
            WriteLine($"Indexed {_objectHash.Count} assets to be extracted.", ConsoleColor.Green);
            return true;
        }
        else
        {
            WriteLine("No assets were loaded. Please make sure there are .json files located in the indexes folder in assets.", ConsoleColor.Red);
            return false;
        }
    }
    else
    {
        WriteLine("The assets folder could not be found. Please make sure you specified the .minecraft directory and that you are using a supported version of Minecraft.", ConsoleColor.Red);
        return false;
    }
}

/// <summary>
/// Extracts the assets into readable filenames
/// </summary>
async Task ExtractAssetsAsync()
{
    var assetsFolder = Path.Combine(_minecraftDir, "assets");
    var objectsFolder = Path.Combine(assetsFolder, "objects");

    WriteLine($"\nExtracting files to {_destinationDir}.", ConsoleColor.Blue);
    // Look up the first two characters of each hash. This represents the folder the asset is located in.
    foreach (var keyValuePair in _objectHash)
    {
        var assetName = keyValuePair.Key;
        // Take firt two characters
        var hashDir = keyValuePair.Value.Substring(0, 2);
        // Look up the file in the directory
        var objectFile = new FileInfo(Path.Combine(objectsFolder, hashDir, keyValuePair.Value));

        // Get the filename portion of the asset name.
        // Break the asset name into parts using the "/"
        var assetNameParts = assetName.Split('/');
        // Create the filename based on the asset paths. Skip the first two because we don't need minecraft/sounds.
        string fileName = string.Join("-", assetNameParts[2..]);
        Write($"{objectFile.Name} -> ");
        WriteLine(fileName, ConsoleColor.Yellow);

        // Copy the file to the destination
        await CopyFileAsync(objectFile.FullName, Path.Combine(_destinationDir, fileName));
    }

    WriteLine("Done!", ConsoleColor.Green);
}

/// <summary>
/// Copies a file from source to destination
/// </summary>
async Task CopyFileAsync(string sourceFile, string destinationFile)
{
    try
    {
        // Open source file as a stream.
        using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        // Create new destination file as a stream.
        using var destinationStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        // copy contents of source to destination.
        await sourceStream.CopyToAsync(destinationStream);
    }
    catch
    {
        WriteLine("Unable to copy file. Perhaps it already exists or you do not have permission.", ConsoleColor.Red);
    }
}
